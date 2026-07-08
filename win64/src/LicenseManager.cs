using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace VideoCompressorUI
{
    internal static class LicenseManager
    {
        public const string LicensePrefix = "YBBPRO";
        private const string ProductName = "YBBvideozip";
        private const string PublicKeyXml = "<RSAKeyValue><Modulus>zo5L7VNa7dbdz7pxwn+4MpiaG3T6mlZseZjmP0JOG4THgiQj9cBBWP5Fbh0vkVOd00eR2c/3nJ3+QrdBY5s7iDVoGXPWH8HjCtNL6/+C4Ifyfdup9HXeQyV+kdCc6qzKoDStWMbYOGq6FQNmYB0oDRBh6uefwurljCtoiGTCj3SpX2NMhn1gZUxxWLj5TezO5hZ9OzPu5Q0m2Ojj8NDtm+ZyM3h3lYViOdD8iKE4km5bD6y/Ehci+k6SugwoFvkK5vLlSIgRWsqwadC7nATTkMmU2lKIsF2lGvZWZ9IgDjlKuZWanqYL+KG6VZlPDl92//NA+P1faE10t0menDeZQQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static string CreateLicenseCode(string payload, string privateKeyXml)
        {
            string normalizedPayload = NormalizePayload(payload);
            if (String.IsNullOrWhiteSpace(privateKeyXml))
            {
                throw new ArgumentException("Private key XML is required.", "privateKeyXml");
            }

            byte[] signature;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKeyXml);
                byte[] bytes = Encoding.UTF8.GetBytes(SigningPayload(normalizedPayload));
                signature = rsa.SignData(bytes, CryptoConfig.MapNameToOID("SHA256"));
            }

            return LicensePrefix + "-" + normalizedPayload + "-" + Base64UrlEncode(signature);
        }

        public static bool IsValidLicenseCode(string code)
        {
            return IsValidLicenseCode(code, PublicKeyXml);
        }

        public static bool IsValidLicenseCode(string code, string publicKeyXml)
        {
            string payload;
            string signatureText;
            if (!TryParseLicenseCode(code, out payload, out signatureText))
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(publicKeyXml))
            {
                return false;
            }

            try
            {
                byte[] signature = Base64UrlDecode(signatureText);
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(publicKeyXml);
                    byte[] bytes = Encoding.UTF8.GetBytes(SigningPayload(payload));
                    return rsa.VerifyData(bytes, CryptoConfig.MapNameToOID("SHA256"), signature);
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsProActivated()
        {
            string code = LoadLicenseCode();
            return IsValidLicenseCode(code);
        }

        public static string LoadLicenseCode()
        {
            try
            {
                string path = GetLicenseFilePath();
                if (File.Exists(path))
                {
                    return File.ReadAllText(path, Encoding.UTF8).Trim();
                }
            }
            catch
            {
            }

            return "";
        }

        public static bool SaveLicenseCode(string code, out string message)
        {
            string normalized = NormalizeLicenseCode(code);
            if (!IsValidLicenseCode(normalized))
            {
                message = "授权码无效。";
                return false;
            }

            try
            {
                string path = GetLicenseFilePath();
                string directory = Path.GetDirectoryName(path);
                if (!String.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, normalized, Encoding.UTF8);
                message = "Pro 已激活。";
                return true;
            }
            catch (Exception ex)
            {
                message = "保存授权状态失败：" + ex.Message;
                return false;
            }
        }

        public static string GetLicenseFilePath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (String.IsNullOrWhiteSpace(localAppData))
            {
                localAppData = Path.GetTempPath();
            }

            return Path.Combine(localAppData, ProductName, "license.dat");
        }

        public static string NormalizeLicenseCode(string code)
        {
            if (String.IsNullOrWhiteSpace(code))
            {
                return "";
            }

            return Regex.Replace(code.Trim(), "\\s+", "");
        }

        private static bool TryParseLicenseCode(string code, out string payload, out string signature)
        {
            payload = "";
            signature = "";
            string normalized = NormalizeLicenseCode(code);
            Match match = Regex.Match(
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
            string value = String.IsNullOrWhiteSpace(payload) ? "" : payload.ToUpperInvariant();
            value = Regex.Replace(value, "[^A-Z0-9]", "");
            if (value.Length < 4)
            {
                throw new ArgumentException("License payload must contain at least 4 letters or digits.", "payload");
            }

            if (value.Length > 32)
            {
                value = value.Substring(0, 32);
            }

            return value;
        }

        private static string SigningPayload(string payload)
        {
            return ProductName + "|pro|v2|" + payload;
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
            string value = text.Replace('-', '+').Replace('_', '/');
            int padding = value.Length % 4;
            if (padding == 2)
            {
                value += "==";
            }
            else if (padding == 3)
            {
                value += "=";
            }
            else if (padding != 0)
            {
                throw new FormatException("Invalid base64url length.");
            }

            return Convert.FromBase64String(value);
        }
    }
}
