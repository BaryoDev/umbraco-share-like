using System.Net;
using BaryoDev.Umbraco.ShareLike.Likes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.DependencyInjection;

namespace BaryoDev.Umbraco.ShareLike.Tests;

public class LikeRateLimitTests
{
    // The composer's own limiter policy, run in front of a stand-in endpoint that carries the same policy name
    // as LikesController.Like.
    private static async Task<IHost> Start(int limit)
    {
        return await new HostBuilder().ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                var builder = System.Reflection.DispatchProxy.Create<IUmbracoBuilder, ShareLikeOptionsTests.BuilderProxy>();
                ((ShareLikeOptionsTests.BuilderProxy)(object)builder).Services = services;
                new ShareLikeComposer().Compose(builder);
                services.Configure<ShareLikeOptions>(o => o.LikePermitLimit = limit);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseRateLimiter();
                app.UseEndpoints(e => e.MapPost("/like", () => Results.Ok())
                    .RequireRateLimiting(LikeRateLimit.PolicyName));
            })).StartAsync();
    }

    [Fact]
    public async Task Like_endpoint_answers_429_after_the_limit()
    {
        using var host = await Start(limit: 3);
        using var client = host.GetTestClient();

        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            codes.Add((await client.PostAsync("/like", null, TestContext.Current.CancellationToken)).StatusCode);
        }

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests],
            codes);
    }

    [Fact]
    public void Same_address_gives_the_same_key_and_another_address_a_different_one()
    {
        var salt = new byte[] { 1, 2, 3 };

        Assert.Equal(
            LikeRateLimit.PartitionKey(IPAddress.Parse("203.0.113.5"), salt),
            LikeRateLimit.PartitionKey(IPAddress.Parse("203.0.113.5"), salt));
        Assert.NotEqual(
            LikeRateLimit.PartitionKey(IPAddress.Parse("203.0.113.5"), salt),
            LikeRateLimit.PartitionKey(IPAddress.Parse("203.0.113.6"), salt));
    }
}
