using System.Runtime.InteropServices;

namespace YBBvideozip.Mac.Platform;

public static class MacPlatformInfo
{
    public const string PlatformName = "macOS";

    public static bool IsArm64 => RuntimeInformation.OSArchitecture == Architecture.Arm64;

    public static string ArchitectureName => IsArm64 ? "arm64" : "x64";
}
