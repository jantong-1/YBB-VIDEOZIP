using System.Globalization;
using System.Text.Json;

namespace YBBvideozip.Mac.Ads;

public static class AdConfigManager
{
    public const string DefaultConfigUrl = "https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ad-config.json";
    public const string DefaultPurchaseUrl = "https://shenlouar.cn/YBBvideozipFFmpeg/pro.html";
    public const string DefaultFallbackVideoUrl = "https://vaers.oss-cn-beijing.aliyuncs.com/YBBffmpegVideo/ShowReel2024_h264.mp4";

    private static readonly Random SharedRandom = new();

    public static async Task<AdConfig> LoadRemoteOrDefaultAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var json = await client.GetStringAsync(DefaultConfigUrl, cancellationToken).ConfigureAwait(false);
            var config = ParseConfig(json);
            return config.Ads.Count > 0 ? config : CreateDefaultConfig();
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    public static AdConfig CreateDefaultConfig()
    {
        var config = new AdConfig();
        config.Ads.Add(new AdItem
        {
            Id = "ybb-default-pro",
            Title = "YBBvideozip Pro",
            VideoUrl = DefaultFallbackVideoUrl,
            ClickUrl = DefaultPurchaseUrl,
            Platform = "macOS",
            AppVersion = "*",
            Enabled = true,
            Weight = 1,
            MinPlaySeconds = 15
        });
        return config;
    }

    public static AdConfig ParseConfig(string json)
    {
        var config = new AdConfig();
        if (string.IsNullOrWhiteSpace(json))
        {
            return config;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var ads = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("ads", out var adsProperty) ? adsProperty : default;

        if (ads.ValueKind != JsonValueKind.Array)
        {
            return config;
        }

        foreach (var item in ads.EnumerateArray())
        {
            var ad = ParseAdItem(item);
            if (!string.IsNullOrWhiteSpace(ad.Id) && !string.IsNullOrWhiteSpace(ad.VideoUrl))
            {
                config.Ads.Add(ad);
            }
        }

        return config;
    }

    public static AdItem SelectAd(AdConfig? config, string platform, string appVersion, Random? random)
    {
        random ??= SharedRandom;
        var candidates = config?.Ads
            .Where(ad => ad.Enabled &&
                         IsWithinSchedule(ad) &&
                         MatchesList(ad.Platform, platform) &&
                         MatchesVersion(ad.AppVersion, appVersion))
            .ToList() ?? [];

        if (candidates.Count == 0)
        {
            candidates = CreateDefaultConfig().Ads;
        }

        var totalWeight = candidates.Sum(ad => Math.Max(1, ad.Weight));
        int pick;
        lock (random)
        {
            pick = random.Next(totalWeight);
        }

        var cursor = 0;
        foreach (var ad in candidates)
        {
            cursor += Math.Max(1, ad.Weight);
            if (pick < cursor)
            {
                return ad;
            }
        }

        return candidates[0];
    }

    public static string BuildTrackedClickUrl(AdItem? ad, string appVersion, string placement, string platform)
    {
        var baseUrl = ad == null || string.IsNullOrWhiteSpace(ad.ClickUrl) ? DefaultPurchaseUrl : ad.ClickUrl;
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var parts = new[]
        {
            "ad_id=" + Uri.EscapeDataString(ad == null ? "unknown" : ad.Id),
            "app_version=" + Uri.EscapeDataString(appVersion),
            "platform=" + Uri.EscapeDataString(platform),
            "source=desktop_app",
            "placement=" + Uri.EscapeDataString(placement)
        };
        return baseUrl + separator + string.Join("&", parts);
    }

    private static AdItem ParseAdItem(JsonElement item)
    {
        return new AdItem
        {
            Id = StringValue(item, "id", ""),
            Title = StringValue(item, "title", "赞助内容"),
            VideoUrl = StringValue(item, "videoUrl", ""),
            CoverUrl = StringValue(item, "coverUrl", ""),
            ClickUrl = StringValue(item, "clickUrl", DefaultPurchaseUrl),
            Platform = StringValue(item, "platform", "macOS"),
            AppVersion = StringValue(item, "appVersion", "*"),
            StartAt = StringValue(item, "startAt", ""),
            EndAt = StringValue(item, "endAt", ""),
            Enabled = BoolValue(item, "enabled", true),
            Weight = Math.Max(1, IntValue(item, "weight", 1)),
            MinPlaySeconds = Math.Max(1, IntValue(item, "minPlaySeconds", 15))
        };
    }

    private static string StringValue(JsonElement item, string key, string fallback)
    {
        return item.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString()
            : fallback;
    }

    private static bool BoolValue(JsonElement item, string key, bool fallback)
    {
        return item.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ValueKind == JsonValueKind.True || (value.ValueKind != JsonValueKind.False && bool.TryParse(value.ToString(), out var parsed) && parsed)
            : fallback;
    }

    private static int IntValue(JsonElement item, string key, int fallback)
    {
        return item.TryGetProperty(key, out var value) &&
               int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool MatchesList(string rule, string actual)
    {
        if (string.IsNullOrWhiteSpace(rule) || rule.Trim() == "*")
        {
            return true;
        }

        return rule.Split(',').Any(part =>
            part.Trim() == "*" || string.Equals(part.Trim(), actual, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesVersion(string rule, string actual)
    {
        if (string.IsNullOrWhiteSpace(rule) || rule.Trim() == "*")
        {
            return true;
        }

        foreach (var raw in rule.Split(','))
        {
            var part = raw.Trim();
            if (part == "*")
            {
                return true;
            }

            if (part.EndsWith('*') &&
                actual.StartsWith(part[..^1], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(part, actual, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWithinSchedule(AdItem ad)
    {
        var now = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(ad.StartAt) &&
            DateTime.TryParse(ad.StartAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var start) &&
            now < start)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(ad.EndAt) &&
            DateTime.TryParse(ad.EndAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var end) &&
            now > end)
        {
            return false;
        }

        return true;
    }
}
