using System.Diagnostics;
using YBBvideozip.Mac.Compression;
using YBBvideozip.Mac.Models;

namespace YBBvideozip.Mac.Tests;

public sealed class FfmpegRunnerSmokeTests
{
    [Fact]
    public async Task CompressesSampleWithConfiguredFfmpegWhenEnvironmentIsSet()
    {
        var ffmpegPath = Environment.GetEnvironmentVariable("YBB_VIDEOZIP_SMOKE_FFMPEG");
        var ffprobePath = Environment.GetEnvironmentVariable("YBB_VIDEOZIP_SMOKE_FFPROBE");
        if (string.IsNullOrWhiteSpace(ffmpegPath) || string.IsNullOrWhiteSpace(ffprobePath))
        {
            return;
        }

        Assert.True(File.Exists(ffmpegPath), "YBB_VIDEOZIP_SMOKE_FFMPEG must point to ffmpeg.");
        Assert.True(File.Exists(ffprobePath), "YBB_VIDEOZIP_SMOKE_FFPROBE must point to ffprobe.");

        var workDir = Path.Combine(Path.GetTempPath(), "YBBvideozip-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        try
        {
            var inputPath = Path.Combine(workDir, "input.mp4");
            await RunProcessAsync(
                ffmpegPath,
                [
                    "-hide_banner",
                    "-loglevel",
                    "error",
                    "-y",
                    "-f",
                    "lavfi",
                    "-i",
                    "testsrc2=duration=1:size=640x360:rate=30",
                    "-f",
                    "lavfi",
                    "-i",
                    "sine=frequency=1000:duration=1",
                    "-c:v",
                    "libx264",
                    "-pix_fmt",
                    "yuv420p",
                    "-c:a",
                    "aac",
                    inputPath
                ]);

            var duration = await new FfprobeRunner().ProbeDurationAsync(ffprobePath, inputPath, CancellationToken.None);
            Assert.True(duration > 0.5, "ffprobe should detect the generated sample duration.");

            var cases = new[]
            {
                (CodecChoice.H264, EngineChoice.Cpu, "cpu-h264.mp4"),
                (CodecChoice.H265, EngineChoice.Cpu, "cpu-h265.mp4"),
                (CodecChoice.H264, EngineChoice.Gpu, "gpu-h264.mp4"),
                (CodecChoice.H265, EngineChoice.Gpu, "gpu-h265.mp4")
            };

            foreach (var (codec, engine, fileName) in cases)
            {
                var outputPath = Path.Combine(workDir, fileName);
                var job = new VideoJob
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    DurationSeconds = duration
                };

                var progressEvents = new List<int>();
                var succeeded = await new FfmpegRunner().RunAsync(
                    ffmpegPath,
                    job,
                    new CompressionOptions(inputPath, outputPath, codec, engine, QualityChoice.Balanced),
                    progressEvents.Add,
                    CancellationToken.None);

                Assert.True(succeeded, $"{codec}/{engine} failed: {job.ErrorMessage}");
                Assert.True(new FileInfo(outputPath).Length > 0, $"{outputPath} should not be empty.");
                Assert.Contains(progressEvents, value => value >= 99);
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    private static async Task RunProcessAsync(string fileName, IReadOnlyList<string> arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        Assert.True(process.ExitCode == 0, $"{fileName} failed with exit {process.ExitCode}.\n{stdout}\n{stderr}");
    }
}
