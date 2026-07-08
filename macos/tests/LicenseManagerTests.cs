using System.Security.Cryptography;
using YBBvideozip.Mac.Licensing;

namespace YBBvideozip.Mac.Tests;

public sealed class LicenseManagerTests
{
    [Fact]
    public void ValidatesGeneratedLocalProLicense()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();

        var code = LicenseManager.CreateLicenseCode("ORDER123", privateKey);

        Assert.True(LicenseManager.IsValidLicenseCode(code, publicKey));
        Assert.False(LicenseManager.IsValidLicenseCode(code + "X", publicKey));
        Assert.False(LicenseManager.IsValidLicenseCode(code.Replace("ORDER123", "ORDER124"), publicKey));
    }
}
