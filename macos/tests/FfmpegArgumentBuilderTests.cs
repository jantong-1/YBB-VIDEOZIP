using System.Globalization;
using YBBvideozip.Mac.Compression;
using YBBvideozip.Mac.Models;

namespace YBBvideozip.Mac.Tests;

public sealed class FfmpegArgumentBuilderTests
{
    [Fact]
    public void BuildsCpuH264ArgumentsMatchingWindowsBehavior()
    {
        var options = new CompressionOptions(
            "/tmp/in.mov",
            "/tmp/out.mp4",
            CodecChoice.H264,
            EngineChoice.Cpu,
            QualityChoice.Balanced);

        var args = FfmpegArgumentBuilder.Build(options);

        Assert.Contains("-c:v", args);
        Assert.Contains("libx264", args);
        Assert.Contains("-crf", args);
        Assert.Contains("25", args);
        Assert.Contains("-c:a", args);
        Assert.Contains("aac", args);
        Assert.Equal("/tmp/in.mov", args[args.IndexOf("-i") + 1]);
        Assert.Equal("/tmp/out.mp4", args[^1]);
    }

    [Fact]
    public void BuildsCpuH265ArgumentsWithHvc1Tag()
    {
        var options = new CompressionOptions(
            "/tmp/in.mov",
            "/tmp/out.mp4",
            CodecChoice.H265,
            EngineChoice.Cpu,
            QualityChoice.Small);

        var args = FfmpegArgumentBuilder.Build(options);

        Assert.Contains("libx265", args);
        Assert.Contains("34", args);
        Assert.Contains("-tag:v", args);
        Assert.Contains("hvc1", args);
    }

    [Fact]
    public void BuildsMacGpuArgumentsWithVideoToolbox()
    {
        var options = new CompressionOptions(
            "/tmp/in.mov",
            "/tmp/out.mp4",
            CodecChoice.H265,
            EngineChoice.Gpu,
            QualityChoice.High);

        var args = FfmpegArgumentBuilder.Build(options);

        Assert.Contains("hevc_videotoolbox", args);
        Assert.DoesNotContain("hevc_nvenc", args);
        Assert.Contains("-q:v", args);
        Assert.Contains(FfmpegArgumentBuilder.GetVideoToolboxQuality(QualityChoice.High).ToString(CultureInfo.InvariantCulture), args);
    }
}
