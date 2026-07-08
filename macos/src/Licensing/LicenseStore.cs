namespace YBBvideozip.Mac.Licensing;

public sealed class LicenseStore
{
    private const string ProductName = "YBBvideozip";

    public string LicenseFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library",
        "Application Support",
        ProductName,
        "license.dat");

    public string Load()
    {
        try
        {
            return File.Exists(LicenseFilePath)
                ? File.ReadAllText(LicenseFilePath).Trim()
                : "";
        }
        catch
        {
            return "";
        }
    }

    public void Save(string code)
    {
        var directory = Path.GetDirectoryName(LicenseFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(LicenseFilePath, code);
    }
}
