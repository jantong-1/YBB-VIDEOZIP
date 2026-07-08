namespace YBBvideozip.Mac.Tests;

public sealed class MainWindowCompletionSourceTests
{
    [Fact]
    public void QueueCompletionShowsReturnButtonBeforeAdIsClosed()
    {
        var source = File.ReadAllText(FindSourceFile("MainWindow.axaml.cs"));

        Assert.Contains("MarkQueueFinished(failures);", source);
        Assert.Contains("SetExecuteButtonState(\"完成\", \"点击返回\")", source);
        Assert.DoesNotContain("if (adGateActive && AdOverlayControl.IsVisible)\r\n            {\r\n                StatusTextControl.Text = CanCloseAd()", source);
    }

    [Fact]
    public void FinishedExecuteButtonStopsAdAndReturnsToMainUi()
    {
        var source = File.ReadAllText(FindSourceFile("MainWindow.axaml.cs"));

        Assert.Contains("if (queueFinished)", source);
        Assert.Contains("StopAdGate();", source);
        Assert.Contains("ClearJobs();", source);
    }

    [Fact]
    public void RunningExecuteButtonUsesSmallerTitleFont()
    {
        var source = File.ReadAllText(FindSourceFile("MainWindow.axaml.cs"));

        Assert.Contains("ExecuteTitleNormalFontSize = 30", source);
        Assert.Contains("ExecuteTitleRunningFontSize = 24", source);
        Assert.Contains("SetExecuteButtonState(\"执行中\", ExecuteTitleRunningFontSize)", source);
        Assert.Contains("ExecuteTitleTextControl.FontSize = titleFontSize;", source);
    }

    [Fact]
    public void AdPlaybackDefaultsToAudible()
    {
        var source = File.ReadAllText(FindSourceFile("MainWindow.axaml.cs"));

        Assert.Contains("DefaultAdMuted = false", source);
        Assert.Contains("adMuted = DefaultAdMuted;", source);
        Assert.Contains("AdHtmlBuilder.Build(currentAd.VideoUrl, adMuted", source);
    }

    [Fact]
    public void HtmlAdMessagesDriveDetailsAndCloseActions()
    {
        var source = File.ReadAllText(FindSourceFile("MainWindow.axaml.cs"));

        Assert.Contains("WebMessageReceived", source);
        Assert.Contains("case \"details\":", source);
        Assert.Contains("case \"close\":", source);
    }

    private static string FindSourceFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "src", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find source file.", fileName);
    }
}
