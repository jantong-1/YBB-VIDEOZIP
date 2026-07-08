using System.Diagnostics;
using System.IO.Compression;
using YBBvideozip.Mac.Platform;

namespace YBBvideozip.Mac.Runtime;

public sealed class RuntimeInstaller
{
    public const string DefaultManifestUrl = "https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/runtime-manifest.json";

    private readonly RuntimeResolver resolver;
    private readonly HttpClient httpClient;

    public RuntimeInstaller(RuntimeResolver resolver, HttpClient? httpClient = null)
    {
        this.resolver = resolver;
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public async Task InstallAsync(IProgress<string> status, CancellationToken cancellationToken)
    {
        status.Report("正在下载 FFmpeg 组件清单...");
        var manifestJson = await httpClient.GetStringAsync(DefaultManifestUrl, cancellationToken).ConfigureAwait(false);
        var manifest = RuntimeManifest.Parse(manifestJson);
        var package = manifest.Select("macOS", resolver.GetArchitecture());

        var tempRoot = Path.Combine(Path.GetTempPath(), "YBBvideozip", Guid.NewGuid().ToString("N"));
        var archivePath = Path.Combine(tempRoot, string.IsNullOrWhiteSpace(package.ArchiveName) ? "ffmpeg-runtime.zip" : package.ArchiveName);
        var extractRoot = Path.Combine(tempRoot, "extract");

        try
        {
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(extractRoot);

            status.Report("正在下载 FFmpeg 组件...");
            await DownloadArchiveAsync(package, archivePath, cancellationToken).ConfigureAwait(false);

            await Sha256Verifier.VerifyAsync(archivePath, package.Sha256, cancellationToken).ConfigureAwait(false);

            status.Report("正在解压 FFmpeg 组件...");
            if (Directory.Exists(resolver.UserRuntimeRoot))
            {
                Directory.Delete(resolver.UserRuntimeRoot, true);
            }

            Directory.CreateDirectory(resolver.UserRuntimeRoot);
            ExtractZipSafely(archivePath, extractRoot);

            var packageRoot = string.IsNullOrWhiteSpace(package.RootDirectory)
                ? extractRoot
                : Path.Combine(extractRoot, package.RootDirectory);
            if (!Directory.Exists(packageRoot))
            {
                packageRoot = extractRoot;
            }

            CopyDirectory(packageRoot, resolver.UserRuntimeRoot);
            await MakeExecutableAsync(Path.Combine(resolver.UserRuntimeRoot, "bin", "ffmpeg"), cancellationToken).ConfigureAwait(false);
            await MakeExecutableAsync(Path.Combine(resolver.UserRuntimeRoot, "bin", "ffprobe"), cancellationToken).ConfigureAwait(false);

            if (!resolver.RuntimeExists())
            {
                throw new FileNotFoundException("压缩包内缺少 ffmpeg 或 ffprobe。");
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    public static IEnumerable<string> GetRuntimeDownloadUrls(RuntimePackage package)
    {
        if (!string.IsNullOrWhiteSpace(package.Url))
        {
            yield return package.Url;
        }

        if (string.Equals(package.Platform, "macOS", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(package.Architecture, "arm64", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(package.ArchiveName))
        {
            var fallback = "https://shenlouarwebsite.oss-cn-shanghai.aliyuncs.com/YBBvideozipFFmpeg/" + package.ArchiveName;
            if (!string.Equals(package.Url, fallback, StringComparison.OrdinalIgnoreCase))
            {
                yield return fallback;
            }
        }
    }

    private async Task DownloadArchiveAsync(RuntimePackage package, string archivePath, CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        foreach (var url in GetRuntimeDownloadUrls(package))
        {
            try
            {
                await using var remote = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
                await using var local = File.Create(archivePath);
                await remote.CopyToAsync(local, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException("无法下载 FFmpeg 组件。", lastError);
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destinationDirectory, Path.GetRelativePath(sourceDirectory, directory));
            Directory.CreateDirectory(target);
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destinationDirectory, Path.GetRelativePath(sourceDirectory, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }

    private static void ExtractZipSafely(string archivePath, string destinationDirectory)
    {
        var destinationFullPath = Path.GetFullPath(destinationDirectory);
        using var archive = ZipFile.OpenRead(archivePath);
        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
            if (!destinationPath.StartsWith(destinationFullPath, StringComparison.Ordinal))
            {
                throw new InvalidDataException("FFmpeg 压缩包包含非法路径。");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, true);
        }
    }

    private static async Task MakeExecutableAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "chmod",
            UseShellExecute = false
        };
        psi.ArgumentList.Add("+x");
        psi.ArgumentList.Add(path);

        using var process = Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
