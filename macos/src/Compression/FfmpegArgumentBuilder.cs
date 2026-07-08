using System.Globalization;
using YBBvideozip.Mac.Models;

namespace YBBvideozip.Mac.Compression;

public static class FfmpegArgumentBuilder
{
    public static List<string> Build(CompressionOptions options)
    {
        var args = new List<string>
        {
            "-y",
            "-hide_banner",
            "-nostats",
            "-progress",
            "pipe:1",
            "-i",
            options.InputPath
        };

        if (options.Engine == EngineChoice.Cpu)
        {
            AddCpuVideoArgs(args, options.Codec, options.Quality);
        }
        else
        {
            AddVideoToolboxArgs(args, options.Codec, options.Quality);
        }

        args.AddRange(["-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", options.OutputPath]);
        return args;
    }

    public static string GetRequiredEncoder(CodecChoice codec, EngineChoice engine)
    {
        if (engine == EngineChoice.Cpu)
        {
            return codec == CodecChoice.H264 ? "libx264" : "libx265";
        }

        return codec == CodecChoice.H264 ? "h264_videotoolbox" : "hevc_videotoolbox";
    }

    public static int GetVideoToolboxQuality(QualityChoice quality)
    {
        return quality switch
        {
            QualityChoice.High => 55,
            QualityChoice.Small => 35,
            _ => 45
        };
    }

    private static void AddCpuVideoArgs(List<string> args, CodecChoice codec, QualityChoice quality)
    {
        if (codec == CodecChoice.H264)
        {
            args.AddRange(["-c:v", "libx264", "-preset", "medium", "-crf", GetH264Crf(quality).ToString(CultureInfo.InvariantCulture), "-threads", "0", "-pix_fmt", "yuv420p"]);
            return;
        }

        args.AddRange(["-c:v", "libx265", "-preset", "medium", "-crf", GetH265Crf(quality).ToString(CultureInfo.InvariantCulture), "-x265-params", "aq-mode=2:ref=3:bframes=3:rc-lookahead=15", "-threads", "0", "-tag:v", "hvc1", "-pix_fmt", "yuv420p"]);
    }

    private static void AddVideoToolboxArgs(List<string> args, CodecChoice codec, QualityChoice quality)
    {
        if (codec == CodecChoice.H264)
        {
            args.AddRange(["-c:v", "h264_videotoolbox", "-q:v", GetVideoToolboxQuality(quality).ToString(CultureInfo.InvariantCulture), "-pix_fmt", "yuv420p"]);
            return;
        }

        args.AddRange(["-c:v", "hevc_videotoolbox", "-q:v", GetVideoToolboxQuality(quality).ToString(CultureInfo.InvariantCulture), "-tag:v", "hvc1", "-pix_fmt", "yuv420p"]);
    }

    private static int GetH264Crf(QualityChoice quality)
    {
        return quality switch
        {
            QualityChoice.High => 21,
            QualityChoice.Small => 29,
            _ => 25
        };
    }

    private static int GetH265Crf(QualityChoice quality)
    {
        return quality switch
        {
            QualityChoice.High => 26,
            QualityChoice.Small => 34,
            _ => 30
        };
    }
}
