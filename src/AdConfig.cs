using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace VideoCompressorUI
{
    internal sealed class AdConfig
    {
        public readonly List<AdItem> Ads = new List<AdItem>();
    }

    internal sealed class AdItem
    {
        public string Id;
        public string Title;
        public string VideoUrl;
        public string CoverUrl;
        public string ClickUrl;
        public string Platform;
        public string AppVersion;
        public string StartAt;
        public string EndAt;
        public bool Enabled = true;
        public int Weight = 1;
        public int MinPlaySeconds = 15;
    }

    internal static class AdConfigManager
    {
        public const string DefaultConfigUrl = "https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ad-config.json";
        public const string DefaultPurchaseUrl = "https://shenlouar.cn/YBBvideozipFFmpeg/pro.html";
        public const string DefaultFallbackVideoUrl = "https://vaers.oss-cn-beijing.aliyuncs.com/YBBffmpegVideo/ShowReel2024_h264.mp4";

        private static readonly Random SharedRandom = new Random();

        public static AdConfig LoadRemoteOrDefault()
        {
            return LoadRemoteOrDefault(DefaultConfigUrl);
        }

        public static AdConfig LoadRemoteOrDefault(string configUrl)
        {
            if (!String.IsNullOrWhiteSpace(configUrl))
            {
                try
                {
                    ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | (SecurityProtocolType)3072;
                    using (TimedWebClient client = new TimedWebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string json = client.DownloadString(configUrl);
                        AdConfig config = ParseConfig(json);
                        if (config.Ads.Count > 0)
                        {
                            return config;
                        }
                    }
                }
                catch
                {
                }
            }

            return CreateDefaultConfig();
        }

        public static AdConfig CreateDefaultConfig()
        {
            AdConfig config = new AdConfig();
            config.Ads.Add(new AdItem
            {
                Id = "ybb-default-pro",
                Title = "YBBvideozip Pro",
                VideoUrl = DefaultFallbackVideoUrl,
                ClickUrl = DefaultPurchaseUrl,
                Platform = "Windows",
                AppVersion = "*",
                Enabled = true,
                Weight = 1,
                MinPlaySeconds = 15
            });
            return config;
        }

        public static AdConfig ParseConfig(string json)
        {
            AdConfig config = new AdConfig();
            if (String.IsNullOrWhiteSpace(json))
            {
                return config;
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            object root = serializer.DeserializeObject(json);
            IEnumerable ads = ExtractAds(root);
            if (ads == null)
            {
                return config;
            }

            foreach (object adObject in ads)
            {
                Dictionary<string, object> adMap = adObject as Dictionary<string, object>;
                if (adMap == null)
                {
                    continue;
                }

                AdItem item = ParseAdItem(adMap);
                if (!String.IsNullOrWhiteSpace(item.Id) && !String.IsNullOrWhiteSpace(item.VideoUrl))
                {
                    config.Ads.Add(item);
                }
            }

            return config;
        }

        public static AdItem SelectAd(AdConfig config, string platform, string appVersion, Random random)
        {
            if (random == null)
            {
                random = SharedRandom;
            }

            List<AdItem> candidates = new List<AdItem>();
            if (config != null && config.Ads != null)
            {
                candidates = config.Ads
                    .Where(delegate(AdItem ad)
                    {
                        return ad != null &&
                               ad.Enabled &&
                               IsWithinSchedule(ad) &&
                               MatchesList(ad.Platform, platform) &&
                               MatchesVersion(ad.AppVersion, appVersion);
                    })
                    .ToList();
            }

            if (candidates.Count == 0)
            {
                candidates = CreateDefaultConfig().Ads;
            }

            int totalWeight = candidates.Sum(delegate(AdItem ad) { return Math.Max(1, ad.Weight); });
            int pick;
            lock (random)
            {
                pick = random.Next(totalWeight);
            }

            int cursor = 0;
            foreach (AdItem ad in candidates)
            {
                cursor += Math.Max(1, ad.Weight);
                if (pick < cursor)
                {
                    return ad;
                }
            }

            return candidates[0];
        }

        public static string BuildTrackedClickUrl(AdItem ad, string appVersion, string placement)
        {
            string baseUrl = ad == null || String.IsNullOrWhiteSpace(ad.ClickUrl) ? DefaultPurchaseUrl : ad.ClickUrl;
            string separator = baseUrl.IndexOf("?", StringComparison.Ordinal) >= 0 ? "&" : "?";
            List<string> parts = new List<string>();
            parts.Add("ad_id=" + Escape(ad == null ? "unknown" : ad.Id));
            parts.Add("app_version=" + Escape(appVersion));
            parts.Add("platform=Windows");
            parts.Add("source=desktop_app");
            parts.Add("placement=" + Escape(placement));
            return baseUrl + separator + String.Join("&", parts.ToArray());
        }

        private static IEnumerable ExtractAds(object root)
        {
            if (root is object[])
            {
                return (object[])root;
            }

            Dictionary<string, object> map = root as Dictionary<string, object>;
            if (map == null || !map.ContainsKey("ads"))
            {
                return null;
            }

            return map["ads"] as IEnumerable;
        }

        private static AdItem ParseAdItem(Dictionary<string, object> map)
        {
            AdItem item = new AdItem();
            item.Id = StringValue(map, "id", "");
            item.Title = StringValue(map, "title", "赞助内容");
            item.VideoUrl = StringValue(map, "videoUrl", "");
            item.CoverUrl = StringValue(map, "coverUrl", "");
            item.ClickUrl = StringValue(map, "clickUrl", DefaultPurchaseUrl);
            item.Platform = StringValue(map, "platform", "Windows");
            item.AppVersion = StringValue(map, "appVersion", "*");
            item.StartAt = StringValue(map, "startAt", "");
            item.EndAt = StringValue(map, "endAt", "");
            item.Enabled = BoolValue(map, "enabled", true);
            item.Weight = Math.Max(1, IntValue(map, "weight", 1));
            item.MinPlaySeconds = Math.Max(1, IntValue(map, "minPlaySeconds", 15));
            return item;
        }

        private static bool MatchesList(string rule, string actual)
        {
            if (String.IsNullOrWhiteSpace(rule) || rule.Trim() == "*")
            {
                return true;
            }

            if (String.IsNullOrWhiteSpace(actual))
            {
                return false;
            }

            string[] parts = rule.Split(',');
            foreach (string raw in parts)
            {
                string part = raw.Trim();
                if (part == "*" || String.Equals(part, actual, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesVersion(string rule, string actual)
        {
            if (String.IsNullOrWhiteSpace(rule) || rule.Trim() == "*")
            {
                return true;
            }

            if (String.IsNullOrWhiteSpace(actual))
            {
                return false;
            }

            string[] parts = rule.Split(',');
            foreach (string raw in parts)
            {
                string part = raw.Trim();
                if (part == "*")
                {
                    return true;
                }
                if (part.EndsWith("*", StringComparison.Ordinal) &&
                    actual.StartsWith(part.Substring(0, part.Length - 1), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (String.Equals(part, actual, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWithinSchedule(AdItem ad)
        {
            DateTime now = DateTime.UtcNow;
            DateTime start;
            if (!String.IsNullOrWhiteSpace(ad.StartAt) &&
                DateTime.TryParse(ad.StartAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out start) &&
                now < start)
            {
                return false;
            }

            DateTime end;
            if (!String.IsNullOrWhiteSpace(ad.EndAt) &&
                DateTime.TryParse(ad.EndAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out end) &&
                now > end)
            {
                return false;
            }

            return true;
        }

        private static string StringValue(Dictionary<string, object> map, string key, string fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
            {
                return fallback;
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static bool BoolValue(Dictionary<string, object> map, string key, bool fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
            {
                return fallback;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            bool parsed;
            return Boolean.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out parsed) ? parsed : fallback;
        }

        private static int IntValue(Dictionary<string, object> map, string key, int fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value) || value == null)
            {
                return fallback;
            }

            int parsed;
            return Int32.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        private static string Escape(string value)
        {
            return Uri.EscapeDataString(value ?? "");
        }

        private sealed class TimedWebClient : WebClient
        {
            public int TimeoutMs = 5000;

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request != null)
                {
                    request.Timeout = TimeoutMs;
                }
                return request;
            }
        }
    }
}
