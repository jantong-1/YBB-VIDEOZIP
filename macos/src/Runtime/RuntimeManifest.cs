using System.Text.Json;
using System.Text.Json.Serialization;

namespace YBBvideozip.Mac.Runtime;

public sealed class RuntimeManifest
{
    [JsonPropertyName("runtimes")]
    public List<RuntimePackage> Runtimes { get; set; } = [];

    public RuntimePackage Select(string platform, string architecture)
    {
        var package = Runtimes.FirstOrDefault(item =>
            string.Equals(item.Platform, platform, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Architecture, architecture, StringComparison.OrdinalIgnoreCase));

        return package ?? throw new InvalidOperationException("runtime-manifest.json 中缺少当前平台的 FFmpeg 包。");
    }

    public static RuntimeManifest Parse(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<RuntimeManifest>(json, options) ?? new RuntimeManifest();
    }
}

public sealed class RuntimePackage
{
    public string Platform { get; set; } = "";
    public string Architecture { get; set; } = "";
    public string ArchiveName { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string RootDirectory { get; set; } = "ffmpeg";
}
