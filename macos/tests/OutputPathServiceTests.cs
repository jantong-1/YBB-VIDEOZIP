using YBBvideozip.Mac.Compression;
using YBBvideozip.Mac.Models;

namespace YBBvideozip.Mac.Tests;

public sealed class OutputPathServiceTests
{
    [Fact]
    public void CreatesCodecSuffixNextToInputWhenNoCustomDirectoryIsSet()
    {
        using var temp = new TemporaryDirectory();
        var input = Path.Combine(temp.Path, "clip.mov");
        File.WriteAllText(input, "not a real video");

        var output = OutputPathService.CreateOutputPath(input, null, CodecChoice.H264);

        Assert.Equal(Path.Combine(temp.Path, "clip_h264.mp4"), output);
    }

    [Fact]
    public void AddsNumberWhenOutputAlreadyExists()
    {
        using var temp = new TemporaryDirectory();
        var input = Path.Combine(temp.Path, "clip.mov");
        var existing = Path.Combine(temp.Path, "clip_h265.mp4");
        File.WriteAllText(input, "not a real video");
        File.WriteAllText(existing, "existing");

        var output = OutputPathService.CreateOutputPath(input, temp.Path, CodecChoice.H265);

        Assert.Equal(Path.Combine(temp.Path, "clip_h265_1.mp4"), output);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ybb-mac-test-" + Guid.NewGuid().ToString("N"));

        public TemporaryDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
