using System.Security.Cryptography;

namespace YBBvideozip.Mac.Runtime;

public static class Sha256Verifier
{
    public static async Task<string> ComputeAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task VerifyAsync(string path, string expectedSha256, CancellationToken cancellationToken)
    {
        var actual = await ComputeAsync(path, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("FFmpeg 组件 SHA256 校验失败。");
        }
    }
}
