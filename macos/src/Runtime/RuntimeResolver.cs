using YBBvideozip.Mac.Platform;

namespace YBBvideozip.Mac.Runtime;

public sealed class RuntimeResolver
{
    private const string ProductName = "YBBvideozip";

    public string UserRuntimeRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library",
        "Application Support",
        ProductName,
        "ffmpeg");

    public string ResolveFfmpegPath()
    {
        return ResolveToolPath("ffmpeg");
    }

    public string ResolveFfprobePath()
    {
        return ResolveToolPath("ffprobe");
    }

    public bool RuntimeExists()
    {
        return File.Exists(ResolveFfmpegPath()) && File.Exists(ResolveFfprobePath());
    }

    public string GetArchitecture()
    {
        return MacPlatformInfo.IsArm64 ? "arm64" : "x64";
    }

    private string ResolveToolPath(string fileName)
    {
        var appNested = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "bin", fileName);
        if (File.Exists(appNested))
        {
            return appNested;
        }

        var userNested = Path.Combine(UserRuntimeRoot, "bin", fileName);
        if (File.Exists(userNested))
        {
            return userNested;
        }

        return Path.Combine(AppContext.BaseDirectory, fileName);
    }
}
