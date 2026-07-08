using System.Diagnostics;

namespace YBBvideozip.Mac.Platform;

public static class MacBrowserLauncher
{
    public static void OpenUrl(string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "open",
            UseShellExecute = false
        };
        psi.ArgumentList.Add(url);
        Process.Start(psi);
    }
}
