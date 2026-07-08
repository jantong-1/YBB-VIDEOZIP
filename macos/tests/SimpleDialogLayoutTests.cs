namespace YBBvideozip.Mac.Tests;

public sealed class SimpleDialogLayoutTests
{
    [Fact]
    public void ConfirmDialogCentersYesNoButtons()
    {
        var source = File.ReadAllText(FindRepoFile("macos", "src", "Controls", "SimpleDialog.cs"));

        Assert.Contains("HorizontalAlignment = HorizontalAlignment.Center", source);
        Assert.Contains("HorizontalContentAlignment = HorizontalAlignment.Center", source);
        Assert.Contains("VerticalContentAlignment = VerticalAlignment.Center", source);
        Assert.Contains("Classes = { \"ButtonLabel\" }", source);
        Assert.Contains("Classes = { \"YbbButton\" }", source);
        Assert.Contains("CreateButtonLabel(text)", source);
        Assert.Contains("CreateButtonLabel(\"确定\")", source);
    }

    private static string FindRepoFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(pathParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find repository file.", Path.Combine(pathParts));
    }
}
