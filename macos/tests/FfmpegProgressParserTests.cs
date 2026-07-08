using YBBvideozip.Mac.Compression;

namespace YBBvideozip.Mac.Tests;

public sealed class FfmpegProgressParserTests
{
    [Fact]
    public void ParsesOutTimeProgress()
    {
        var progress = FfmpegProgressParser.ParseProgress("out_time=00:01:30.000000", 180);

        Assert.Equal(50, progress);
    }

    [Fact]
    public void CapsInFlightProgressAtNinetyNine()
    {
        var progress = FfmpegProgressParser.ParseProgress("out_time=00:03:00.000000", 180);

        Assert.Equal(99, progress);
    }

    [Fact]
    public void ParsesEndProgressAsOneHundred()
    {
        var progress = FfmpegProgressParser.ParseProgress("progress=end", 180);

        Assert.Equal(100, progress);
    }
}
