using BaryoDev.Umbraco.ShareLike.Components;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace BaryoDev.Umbraco.ShareLike.Tests;

public class ShareLinksViewComponentTests
{
    public class Stub : System.Reflection.DispatchProxy
    {
        public Func<string, object?> Answer { get; set; } = _ => null;

        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args) =>
            Answer(targetMethod!.Name);
    }

    private static T Make<T>(Func<string, object?> answer) where T : class
    {
        var proxy = System.Reflection.DispatchProxy.Create<T, Stub>();
        ((Stub)(object)proxy).Answer = answer;
        return proxy;
    }

    private sealed class Monitor(ShareLikeOptions value) : IOptionsMonitor<ShareLikeOptions>
    {
        public ShareLikeOptions CurrentValue => value;

        public ShareLikeOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<ShareLikeOptions, string?> listener) => null;
    }

    [Fact]
    public void Content_with_no_url_renders_nothing()
    {
        var urls = Make<IPublishedUrlProvider>(m => m == "GetUrl" ? "#" : null);
        var content = Make<IPublishedContent>(m => m == "get_Name" ? "A post" : null);
        var component = new ShareLinksViewComponent(new Monitor(new ShareLikeOptions { ShareTargets = ["x"] }), urls);

        var result = component.Invoke(content);

        Assert.IsType<ContentViewComponentResult>(result);
    }
}
