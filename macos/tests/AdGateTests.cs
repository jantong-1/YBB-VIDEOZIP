using YBBvideozip.Mac.Ads;

namespace YBBvideozip.Mac.Tests;

public sealed class AdGateTests
{
    [Fact]
    public void GateShowsCountdownBeforeMinimumAndAllowsCloseAtMinimum()
    {
        var startedAt = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);
        var gate = AdGateState.Start(new AdItem { MinPlaySeconds = 15 }, startedAt);

        Assert.Equal("15 秒后可关闭", gate.CountdownText(startedAt));
        Assert.Equal("1 秒后可关闭", gate.CountdownText(startedAt.AddSeconds(14)));
        Assert.False(gate.CanClose(startedAt.AddSeconds(14.9)));

        Assert.Equal("可以关闭广告", gate.CountdownText(startedAt.AddSeconds(15)));
        Assert.True(gate.CanClose(startedAt.AddSeconds(15)));
    }

    [Fact]
    public void GateRaisesMinimumReachedOnlyOnce()
    {
        var startedAt = new DateTime(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);
        var gate = AdGateState.Start(new AdItem { MinPlaySeconds = 15 }, startedAt);

        Assert.False(gate.TakeMinimumReached(startedAt.AddSeconds(14)));
        Assert.True(gate.TakeMinimumReached(startedAt.AddSeconds(15)));
        Assert.False(gate.TakeMinimumReached(startedAt.AddSeconds(16)));
    }

    [Fact]
    public void HtmlDefaultsVideoToAudiblePlayback()
    {
        var html = AdHtmlBuilder.Build("https://example.com/ad.mp4", muted: false);

        Assert.DoesNotContain(" autoplay playsinline muted", html);
        Assert.Contains("video.muted = false", html);
        Assert.Contains("video.volume = 1", html);
        Assert.Contains(">静音<", html);
    }

    [Fact]
    public void HtmlCoversTheAvailableAdSurfaceWithoutInternalRoundedFrame()
    {
        var html = AdHtmlBuilder.Build("https://example.com/ad.mp4", muted: false);

        Assert.Contains("object-fit:cover", html);
        Assert.Contains("position:absolute", html);
        Assert.Contains("inset:0", html);
        Assert.Contains(".video-clip", html);
        Assert.Contains("inset:3px", html);
        Assert.Contains("border-radius:13px", html);
        Assert.Contains("overflow:hidden", html);
        Assert.DoesNotContain(".stage", html);
        Assert.DoesNotContain("class=\"stage\"", html);
    }

    [Fact]
    public void HtmlDrawsDashedFrameInSameNativeLayerAsVideo()
    {
        var html = AdHtmlBuilder.Build("https://example.com/ad.mp4", muted: false);

        Assert.Contains(".dash-frame", html);
        Assert.Contains("border:2px dashed #111", html);
        Assert.Contains("border-radius:15px", html);
        Assert.Contains("z-index:5", html);
    }

    [Fact]
    public void HtmlPostsCloseAndDetailsActionsToNativeHost()
    {
        var html = AdHtmlBuilder.Build("https://example.com/ad.mp4", muted: false);

        Assert.Contains("post('details')", html);
        Assert.Contains("post('close')", html);
        Assert.Contains("window.chrome.webview.postMessage", html);
        Assert.Contains("window.webkit.messageHandlers.ybb.postMessage", html);
    }

    [Fact]
    public void HtmlButtonsUseFixedLineHeightForVerticalCentering()
    {
        var html = AdHtmlBuilder.Build("https://example.com/ad.mp4", muted: false);

        Assert.Contains("height:30px", html);
        Assert.Contains("line-height:30px", html);
    }
}
