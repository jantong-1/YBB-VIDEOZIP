namespace YBBvideozip.Mac.Tests;

public sealed class PackageScriptTests
{
    [Fact]
    public void LocalAppPackageDoesNotBundleFfmpegRuntime()
    {
        var script = File.ReadAllText(FindRepoFile("macos", "scripts", "package-local-app.sh"));

        Assert.DoesNotContain("command -v ffmpeg", script);
        Assert.DoesNotContain("command -v ffprobe", script);
        Assert.DoesNotContain("ffmpeg/bin", script);
        Assert.Contains("runtime-manifest.json", script);
    }

    [Fact]
    public void LocalAppPackageUsesRepoDotnetWhenAvailable()
    {
        var script = File.ReadAllText(FindRepoFile("macos", "scripts", "package-local-app.sh"));

        Assert.Contains("REPO_DOTNET", script);
        Assert.Contains("tools/dotnet/dotnet", script);
        Assert.Contains("\"$DOTNET\" publish", script);
    }

    [Fact]
    public void LocalAppPackageFailsIfIconIsNotGenerated()
    {
        var script = File.ReadAllText(FindRepoFile("macos", "scripts", "package-local-app.sh"));

        Assert.Contains("CFBundleIconFile", script);
        Assert.Contains("YBBvideozip.icns", script);
        Assert.Contains("if [[ ! -f \"$ICNS\" ]]", script);
        Assert.Contains("Missing app icon", script);
    }

    [Fact]
    public void LocalAppPackageWritesExplicitIconMetadataAndFullIconSet()
    {
        var script = File.ReadAllText(FindRepoFile("macos", "scripts", "package-local-app.sh"));

        Assert.Contains("sips -z 1024 1024", script);
        Assert.Contains("icon_512x512@2x.png", script);
        Assert.Contains("<key>CFBundleIconFile</key>", script);
        Assert.Contains("<string>YBBvideozip</string>", script);
        Assert.DoesNotContain("CFBundleIconName", script);
        Assert.Contains("<string>1.1.1</string>", script);
    }

    [Fact]
    public void NotarizationScriptUsesDeveloperIdAndHardenedRuntime()
    {
        var script = File.ReadAllText(FindRepoFile("macos", "scripts", "sign-notarize-app.sh"));

        Assert.Contains("Developer ID Application", script);
        Assert.Contains("--options runtime", script);
        Assert.Contains("entitlements.plist", script);
        Assert.Contains("file \"$code_file\" | grep -q \"Mach-O\"", script);
        Assert.Contains("notarytool submit", script);
        Assert.Contains("stapler staple", script);
        Assert.Contains("syspolicy_check distribution", script);
        Assert.Contains("spctl --assess --type open", script);
    }

    [Fact]
    public void NotarizationEntitlementsAllowDotnetRuntime()
    {
        var entitlements = File.ReadAllText(FindRepoFile("macos", "entitlements.plist"));

        Assert.Contains("com.apple.security.cs.allow-jit", entitlements);
        Assert.Contains("com.apple.security.cs.allow-unsigned-executable-memory", entitlements);
        Assert.Contains("com.apple.security.cs.disable-library-validation", entitlements);
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
