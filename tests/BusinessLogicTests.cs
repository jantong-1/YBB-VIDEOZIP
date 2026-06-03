using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace VideoCompressorUI
{
    internal static class BusinessLogicTests
    {
        private static int failures;

        private static void Main()
        {
            Run("parses enabled ad list from json", ParsesEnabledAdListFromJson);
            Run("default config url uses oss json", DefaultConfigUrlUsesOssJson);
            Run("default fallback ad uses public video url", DefaultFallbackAdUsesPublicVideoUrl);
            Run("selects enabled platform-compatible ad", SelectsEnabledPlatformCompatibleAd);
            Run("adds tracking parameters to click url", AddsTrackingParametersToClickUrl);
            Run("validates generated local pro license", ValidatesGeneratedLocalProLicense);
            Run("ad playback starts with sound by default", AdPlaybackStartsWithSoundByDefault);

            if (failures > 0)
            {
                Environment.Exit(1);
            }
        }

        private static void DefaultConfigUrlUsesOssJson()
        {
            AssertEqual("https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ad-config.json", AdConfigManager.DefaultConfigUrl, "default config url");
        }

        private static void DefaultFallbackAdUsesPublicVideoUrl()
        {
            AdConfig config = AdConfigManager.CreateDefaultConfig();

            AssertEqual("https://vaers.oss-cn-beijing.aliyuncs.com/YBBffmpegVideo/ShowReel2024_h264.mp4", config.Ads[0].VideoUrl, "default video url");
        }

        private static void ParsesEnabledAdListFromJson()
        {
            string json = "{ \"ads\": [ { \"id\": \"a1\", \"title\": \"Ad One\", \"videoUrl\": \"https://example.com/a.mp4\", \"clickUrl\": \"https://example.com/buy\", \"enabled\": true, \"weight\": 3, \"minPlaySeconds\": 15, \"platform\": \"Windows\" } ] }";
            AdConfig config = AdConfigManager.ParseConfig(json);

            AssertEqual(1, config.Ads.Count, "ad count");
            AssertEqual("a1", config.Ads[0].Id, "id");
            AssertEqual("https://example.com/a.mp4", config.Ads[0].VideoUrl, "video url");
            AssertEqual(15, config.Ads[0].MinPlaySeconds, "min play seconds");
        }

        private static void SelectsEnabledPlatformCompatibleAd()
        {
            AdConfig config = new AdConfig();
            config.Ads.Add(new AdItem { Id = "disabled", Enabled = false, Platform = "Windows", Weight = 100 });
            config.Ads.Add(new AdItem { Id = "mac", Enabled = true, Platform = "macOS", Weight = 100 });
            config.Ads.Add(new AdItem { Id = "win", Enabled = true, Platform = "Windows", Weight = 1 });

            AdItem selected = AdConfigManager.SelectAd(config, "Windows", "1.0.0", new Random(1));

            AssertEqual("win", selected.Id, "selected ad");
        }

        private static void AddsTrackingParametersToClickUrl()
        {
            AdItem ad = new AdItem { Id = "ad-9", ClickUrl = "https://example.com/buy?x=1" };
            string url = AdConfigManager.BuildTrackedClickUrl(ad, "1.2.3", "compress_waiting");

            AssertContains(url, "x=1", "original query");
            AssertContains(url, "ad_id=ad-9", "ad id");
            AssertContains(url, "app_version=1.2.3", "app version");
            AssertContains(url, "platform=Windows", "platform");
            AssertContains(url, "source=desktop_app", "source");
            AssertContains(url, "placement=compress_waiting", "placement");
        }

        private static void ValidatesGeneratedLocalProLicense()
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                string privateKeyXml = rsa.ToXmlString(true);
                string publicKeyXml = rsa.ToXmlString(false);
                string code = LicenseManager.CreateLicenseCode("ORDER123", privateKeyXml);

                AssertTrue(LicenseManager.IsValidLicenseCode(code, publicKeyXml), "generated code should be valid");
                AssertTrue(!LicenseManager.IsValidLicenseCode(code + "X", publicKeyXml), "tampered signature should be invalid");
                AssertTrue(!LicenseManager.IsValidLicenseCode(code.Replace("ORDER123", "ORDER124"), publicKeyXml), "tampered payload should be invalid");
            }
        }

        private static void AdPlaybackStartsWithSoundByDefault()
        {
            string sourcePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src", "AdDisplayPanel.cs"));
            string source = File.ReadAllText(sourcePath);

            AssertContains(source, "private bool muted = false;", "initial muted state");
            AssertContains(source, "muted = false;", "start ad muted state");
            AssertContains(source, "soundButton.Text = SoundText;", "sound button label");
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine("FAIL " + name + ": " + ex.Message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string name)
        {
            if (!Object.Equals(expected, actual))
            {
                throw new InvalidOperationException(name + " expected " + expected + " but got " + actual);
            }
        }

        private static void AssertContains(string text, string expected, string name)
        {
            if (text == null || text.IndexOf(expected, StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(name + " missing " + expected + " in " + text);
            }
        }

        private static void AssertTrue(bool value, string name)
        {
            if (!value)
            {
                throw new InvalidOperationException(name);
            }
        }
    }
}
