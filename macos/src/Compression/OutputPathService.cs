using YBBvideozip.Mac.Models;

namespace YBBvideozip.Mac.Compression;

public static class OutputPathService
{
    public static string CreateOutputPath(string inputPath, string? customOutputDirectory, CodecChoice codec)
    {
        var folder = string.IsNullOrWhiteSpace(customOutputDirectory)
            ? Path.GetDirectoryName(inputPath)
            : customOutputDirectory;

        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new IOException("无法确定输出目录。");
        }

        Directory.CreateDirectory(folder);

        var name = Path.GetFileNameWithoutExtension(inputPath);
        var suffix = codec == CodecChoice.H264 ? "_h264" : "_h265";
        var candidate = Path.Combine(folder, name + suffix + ".mp4");
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        for (var i = 1; i < 1000; i++)
        {
            var numbered = Path.Combine(folder, name + suffix + "_" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".mp4");
            if (!File.Exists(numbered))
            {
                return numbered;
            }
        }

        throw new IOException("无法生成唯一输出文件名。");
    }
}
