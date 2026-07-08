using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace YBBvideozip.Mac.Licensing;

public static class LicenseManager
{
    public const string LicensePrefix = "YBBPRO";
    private const string ProductName = "YBBvideozip";
    private const string PublicKeyXml = "<RSAKeyValue><Modulus>zo5L7VNa7dbdz7pxwn+4MpiaG3T6mlZseZjmP0JOG4THgiQj9cBBWP5Fbh0vkVOd00eR2c/3nJ3+QrdBY5s7iDVoGXPWH8HjCtNL6/+C4Ifyfdup9HXeQyV+kdCc6qzKoDStWMbYOGq6FQNmYB0oDRBh6uefwurljCtoiGTCj3SpX2NMhn1gZUxxWLj5TezO5hZ9OzPu5Q0m2Ojj8NDtm+ZyM3h3lYViOdD8iKE4km5bD6y/Ehci+k6SugwoFvkK5vLlSIgRWsqwadC7nATTkMmU2lKIsF2lGvZWZ9IgDjlKuZWanqYL+KG6VZlPDl92//NA+P1faE10t0menDeZQQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    public static string CreateLicenseCode(string payload, string privateKeyText)
    {
        var normalizedPayload = NormalizePayload(payload);
        using var rsa = CreateRsaFromPrivateKey(privateKeyText);
        var bytes = Encoding.UTF8.GetBytes(SigningPayload(normalizedPayload));
        var signature = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return LicensePrefix + "-" + normalizedPayload + "-" + Base64UrlEncode(signature);
    }

    public static bool IsValidLicenseCode(string code)
    {
        return IsValidLicenseCode(code, PublicKeyXml);
    }

    public static bool IsValidLicenseCode(string code, string publicKeyText)
    {
        if (!TryParseLicenseCode(code, out var payload, out var signatureText))
        {
            return false;
        }

        try
        {
            using var rsa = CreateRsaFromPublicKey(publicKeyText);
            var bytes = Encoding.UTF8.GetBytes(SigningPayload(payload));
            var signature = Base64UrlDecode(signatureText);
            return rsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsProActivated(LicenseStore store)
    {
        return IsValidLicenseCode(store.Load());
    }

    public static bool SaveLicenseCode(string code, LicenseStore store, out string message)
    {
        var normalized = NormalizeLicenseCode(code);
        if (!IsValidLicenseCode(normalized))
        {
            message = "授权码无效。";
            return false;
        }

        try
        {
            store.Save(normalized);
            message = "Pro 已激活。";
            return true;
        }
        catch (Exception ex)
        {
            message = "保存授权状态失败：" + ex.Message;
            return false;
        }
    }

    public static string NormalizeLicenseCode(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? "" : Regex.Replace(code.Trim(), "\\s+", "");
    }

    private static bool TryParseLicenseCode(string code, out string payload, out string signature)
    {
        payload = "";
        signature = "";
        var normalized = NormalizeLicenseCode(code);
        var match = Regex.Match(
            normalized,
            "^" + LicensePrefix + "-([A-Z0-9]{4,32})-([A-Za-z0-9_-]{80,512})$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return false;
        }

        payload = match.Groups[1].Value.ToUpperInvariant();
        signature = match.Groups[2].Value;
        return true;
    }

    private static string NormalizePayload(string payload)
    {
        var value = string.IsNullOrWhiteSpace(payload) ? "" : payload.ToUpperInvariant();
        value = Regex.Replace(value, "[^A-Z0-9]", "");
        if (value.Length < 4)
        {
            throw new ArgumentException("License payload must contain at least 4 letters or digits.", nameof(payload));
        }

        return value.Length > 32 ? value[..32] : value;
    }

    private static string SigningPayload(string payload)
    {
        return ProductName + "|pro|v2|" + payload;
    }

    private static RSA CreateRsaFromPrivateKey(string privateKeyText)
    {
        if (string.IsNullOrWhiteSpace(privateKeyText))
        {
            throw new ArgumentException("Private key is required.", nameof(privateKeyText));
        }

        var rsa = RSA.Create();
        if (privateKeyText.Contains("BEGIN", StringComparison.Ordinal))
        {
            rsa.ImportFromPem(privateKeyText);
            return rsa;
        }

        rsa.ImportParameters(ParseXmlRsaParameters(privateKeyText, true));
        return rsa;
    }

    private static RSA CreateRsaFromPublicKey(string publicKeyText)
    {
        if (string.IsNullOrWhiteSpace(publicKeyText))
        {
            throw new ArgumentException("Public key is required.", nameof(publicKeyText));
        }

        var rsa = RSA.Create();
        if (publicKeyText.Contains("BEGIN", StringComparison.Ordinal))
        {
            rsa.ImportFromPem(publicKeyText);
            return rsa;
        }

        rsa.ImportParameters(ParseXmlRsaParameters(publicKeyText, false));
        return rsa;
    }

    private static RSAParameters ParseXmlRsaParameters(string xml, bool includePrivate)
    {
        var root = XElement.Parse(xml);
        byte[] Read(string name)
        {
            var value = root.Element(name)?.Value;
            return string.IsNullOrWhiteSpace(value) ? [] : Convert.FromBase64String(value);
        }

        var parameters = new RSAParameters
        {
            Modulus = Read("Modulus"),
            Exponent = Read("Exponent")
        };

        if (includePrivate)
        {
            parameters.P = Read("P");
            parameters.Q = Read("Q");
            parameters.DP = Read("DP");
            parameters.DQ = Read("DQ");
            parameters.InverseQ = Read("InverseQ");
            parameters.D = Read("D");
        }

        return parameters;
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string text)
    {
        var value = text.Replace('-', '+').Replace('_', '/');
        var padding = value.Length % 4;
        value += padding switch
        {
            2 => "==",
            3 => "=",
            0 => "",
            _ => throw new FormatException("Invalid base64url length.")
        };

        return Convert.FromBase64String(value);
    }
}
