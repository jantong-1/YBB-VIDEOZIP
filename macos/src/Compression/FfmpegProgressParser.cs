using System.Globalization;

namespace YBBvideozip.Mac.Compression;

public static class FfmpegProgressParser
{
    public static int? ParseProgress(string line, double durationSeconds)
    {
        if (line.Trim().Equals("progress=end", StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (!line.StartsWith("out_time=", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (durationSeconds <= 0)
        {
            return null;
        }

        var seconds = ParseOutTimeSeconds(line["out_time=".Length..]);
        if (seconds < 0)
        {
            return null;
        }

        var progress = (int)Math.Floor(seconds * 100.0 / durationSeconds);
        return Math.Clamp(progress, 0, 99);
    }

    public static double ParseOutTimeSeconds(string value)
    {
        var parts = value.Trim().Split(':');
        if (parts.Length != 3)
        {
            return -1;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
        {
            return -1;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            return -1;
        }

        if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return -1;
        }

        return hours * 3600 + minutes * 60 + seconds;
    }
}
