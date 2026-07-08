using YBBvideozip.Mac.Runtime;

namespace YBBvideozip.Mac.Tests;

public sealed class RuntimeInstallerConfigTests
{
    [Fact]
    public void RuntimeInstallerReadsManifestFromPublicOssBucket()
    {
        Assert.Equal(
            "https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/runtime-manifest.json",
            RuntimeInstaller.DefaultManifestUrl);
    }

    [Fact]
    public void RuntimeManifestUsesPublicOssBucketForMacArm64Runtime()
    {
        var manifestJson = File.ReadAllText(FindRepoFile("oss", "YBBvideozipFFmpeg", "runtime-manifest.json"));
        var manifest = RuntimeManifest.Parse(manifestJson);
        var package = manifest.Select("macOS", "arm64");

        Assert.Equal("ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip", package.ArchiveName);
        Assert.Equal(
            "https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip",
            package.Url);
        Assert.Equal("e9e5e4010150ab4a51aa4e75175dda27b5e3a3072327ea12fed98ef3d60c3bd1", package.Sha256);
    }

    [Fact]
    public void RuntimeInstallerFallsBackToPublicOssBucketForMacArm64Runtime()
    {
        var package = new RuntimePackage
        {
            Platform = "macOS",
            Architecture = "arm64",
            ArchiveName = "ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip",
            Url = "https://shenlouar.cn/YBBvideozipFFmpeg/ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip"
        };

        var urls = RuntimeInstaller.GetRuntimeDownloadUrls(package).ToArray();

        Assert.Contains("https://shenlouar.cn/YBBvideozipFFmpeg/ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip", urls);
        Assert.Contains(
            "https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/ffmpeg-runtime-macos-arm64-gpl-8.1.2-ybb.zip",
            urls);
    }

    private static string FindRepoFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(pathParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find repository file.", Path.Combine(pathParts));
    }
}
