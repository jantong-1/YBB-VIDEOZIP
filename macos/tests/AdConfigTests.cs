using YBBvideozip.Mac.Ads;

namespace YBBvideozip.Mac.Tests;

public sealed class AdConfigTests
{
    [Fact]
    public void SelectsEnabledMacCompatibleAd()
    {
        var config = new AdConfig();
        config.Ads.Add(new AdItem { Id = "win", Enabled = true, Platform = "Windows", Weight = 100, VideoUrl = "https://example.com/win.mp4" });
        config.Ads.Add(new AdItem { Id = "mac", Enabled = true, Platform = "macOS", Weight = 1, VideoUrl = "https://example.com/mac.mp4" });

        var selected = AdConfigManager.SelectAd(config, "macOS", "1.0.0", new Random(1));

        Assert.Equal("mac", selected.Id);
    }

    [Fact]
    public void TrackedClickUrlUsesMacPlatform()
    {
        var ad = new AdItem { Id = "ad-9", ClickUrl = "https://example.com/buy?x=1" };

        var url = AdConfigManager.BuildTrackedClickUrl(ad, "1.2.3", "compress_waiting", "macOS");

        Assert.Contains("x=1", url);
        Assert.Contains("ad_id=ad-9", url);
        Assert.Contains("app_version=1.2.3", url);
        Assert.Contains("platform=macOS", url);
        Assert.Contains("source=desktop_app", url);
        Assert.Contains("placement=compress_waiting", url);
    }
}
