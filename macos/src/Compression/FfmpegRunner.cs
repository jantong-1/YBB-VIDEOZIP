using System.Diagnostics;
using System.Text;
using YBBvideozip.Mac.Models;

namespace YBBvideozip.Mac.Compression;

public sealed class FfmpegRunner
{
    public async Task<bool> RunAsync(
        string ffmpegPath,
        VideoJob job,
        CompressionOptions options,
        Action<int> onProgress,
        CancellationToken cancellationToken)
    {
        var args = FfmpegArgumentBuilder.Build(options);
        var recentErrors = new Queue<string>();

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        try
        {
            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                var progress = FfmpegProgressParser.ParseProgress(e.Data, job.DurationSeconds);
                if (progress.HasValue)
                {
                    onProgress(progress.Value);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                }

                lock (recentErrors)
                {
                    if (recentErrors.Count >= 8)
                    {
                        recentErrors.Dequeue();
                    }

                    recentErrors.Enqueue(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode == 0 && File.Exists(options.OutputPath))
            {
                return true;
            }

            lock (recentErrors)
            {
                job.ErrorMessage = recentErrors.Count == 0 ? "FFmpeg 返回失败。" : recentErrors.Last();
            }

            return false;
        }
        catch (Exception ex)
        {
            job.ErrorMessage = ex.Message;
            return false;
        }
    }
}
