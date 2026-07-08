using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace YBBvideozip.Mac.Compression;

public sealed class FfprobeRunner
{
    public async Task<double> ProbeDurationAsync(string ffprobePath, string inputPath, CancellationToken cancellationToken)
    {
        try
        {
            var output = await RunCaptureAsync(
                ffprobePath,
                ["-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", inputPath],
                TimeSpan.FromSeconds(15),
                cancellationToken).ConfigureAwait(false);

            return double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<string> RunCaptureAsync(string fileName, IEnumerable<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 ffprobe。");
        var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

        await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        return await outputTask.ConfigureAwait(false) + "\n" + await errorTask.ConfigureAwait(false);
    }
}
