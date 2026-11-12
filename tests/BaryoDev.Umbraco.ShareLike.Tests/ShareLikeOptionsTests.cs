using BaryoDev.Umbraco.ShareLike;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;

namespace BaryoDev.Umbraco.ShareLike.Tests;

public class ShareLikeOptionsTests
{
    // IUmbracoBuilder is an interface, so a proxy that only answers Services is enough for Compose.
    public class BuilderProxy : System.Reflection.DispatchProxy
    {
        public IServiceCollection? Services { get; set; }

        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Services")
            {
                return Services;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private static ShareLikeOptions Resolve(Action<IServiceCollection>? arrange = null, Dictionary<string, string?>? config = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(config ?? []).Build());
        var builder = System.Reflection.DispatchProxy.Create<IUmbracoBuilder, BuilderProxy>();
        ((BuilderProxy)(object)builder).Services = services;

        new ShareLikeComposer().Compose(builder);
        arrange?.Invoke(services);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ShareLikeOptions>>().Value;
    }

    [Fact]
    public void No_share_targets_configured_gives_the_default_list()
    {
        var options = Resolve();

        Assert.Equal(["linkedin", "x", "facebook", "email", "native", "copy"], options.ShareTargets);
    }

    [Fact]
    public void Configured_targets_replace_the_default()
    {
        var options = Resolve(config: new() { ["BaryoDev:ShareLike:ShareTargets:0"] = "bluesky" });

        Assert.Equal(["bluesky"], options.ShareTargets);
    }

    [Fact]
    public void Null_and_blank_targets_are_dropped()
    {
        var options = Resolve(s => s.Configure<ShareLikeOptions>(o => o.ShareTargets = [null!, "", "  ", "x", "copy"]));

        Assert.Equal(["x", "copy"], options.ShareTargets);
    }

    [Fact]
    public void Only_blank_targets_fall_back_to_the_default_list()
    {
        var options = Resolve(s => s.Configure<ShareLikeOptions>(o => o.ShareTargets = [null!, " "]));

        Assert.Equal(ShareLikeOptions.DefaultShareTargets, options.ShareTargets);
    }

    [Theory]
    [InlineData("mastodon.social")]
    [InlineData("social.example.org")]
    public void A_bare_host_is_accepted(string host)
    {
        var options = Resolve(config: new() { ["BaryoDev:ShareLike:MastodonInstance"] = host });

        Assert.Equal(host, options.MastodonInstance);
    }

    [Theory]
    [InlineData("https://mastodon.social")]
    [InlineData("mastodon.social/path")]
    [InlineData("mastodon.social?x=1")]
    [InlineData("user@evil.example")]
    [InlineData("mastodon.social:8443")]
    public void A_host_with_a_scheme_path_port_or_credentials_fails_validation(string host)
    {
        Assert.Throws<OptionsValidationException>(() =>
            Resolve(config: new() { ["BaryoDev:ShareLike:MastodonInstance"] = host }));
    }

    [Fact]
    public void A_blank_instance_is_fine_when_mastodon_is_not_a_target()
    {
        var options = Resolve(config: new() { ["BaryoDev:ShareLike:MastodonInstance"] = "" });

        Assert.Equal("", options.MastodonInstance);
    }

    [Fact]
    public void A_blank_instance_fails_when_mastodon_is_a_target()
    {
        Assert.Throws<OptionsValidationException>(() => Resolve(config: new()
        {
            ["BaryoDev:ShareLike:MastodonInstance"] = "",
            ["BaryoDev:ShareLike:ShareTargets:0"] = "mastodon",
        }));
    }
}
