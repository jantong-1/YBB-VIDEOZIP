using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YBBvideozip.Mac.Ads;
using YBBvideozip.Mac.Compression;
using YBBvideozip.Mac.Controls;
using YBBvideozip.Mac.Licensing;
using YBBvideozip.Mac.Models;
using YBBvideozip.Mac.Platform;
using YBBvideozip.Mac.Runtime;

namespace YBBvideozip.Mac;

public sealed partial class MainWindow : Window
{
    private const string AppVersion = "1.1.1";
    private const string AdMessageHandlerName = "ybb";
    private const bool DefaultAdMuted = false;
    private const double ExecuteTitleNormalFontSize = 30;
    private const double ExecuteTitleRunningFontSize = 24;

    private static readonly string[] SupportedExtensions =
    [
        ".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"
    ];

    private readonly RuntimeResolver runtimeResolver = new();
    private readonly LicenseStore licenseStore = new();
    private readonly FfprobeRunner ffprobeRunner = new();
    private readonly FfmpegRunner ffmpegRunner = new();
    private readonly DispatcherTimer adTimer;

    private string ffmpegPath = "";
    private string ffprobePath = "";
    private string encoderList = "";
    private string? customOutputDirectory;
    private bool toolsReady;
    private bool isRunning;
    private bool queueFinished;
    private bool queueWorkFinished;
    private bool adGateActive;
    private int pendingFailures;
    private AdConfig adConfig = AdConfigManager.CreateDefaultConfig();
    private AdItem? currentAd;
    private AdGateState? adGateState;
    private bool adMuted = DefaultAdMuted;
    private bool proActivated;
    private CodecChoice selectedCodec = CodecChoice.H264;
    private EngineChoice selectedEngine = EngineChoice.Cpu;
    private QualityChoice selectedQuality = QualityChoice.Balanced;

    public ObservableCollection<VideoJob> Jobs { get; } = [];

    private Button ProButtonControl => this.FindControl<Button>("ProButton")!;
    private Control DropAreaControl => this.FindControl<Control>("DropArea")!;
    private TextBlock HintTextControl => this.FindControl<TextBlock>("HintText")!;
    private Control JobScrollControl => this.FindControl<Control>("JobScroll")!;
    private Button H264ButtonControl => this.FindControl<Button>("H264Button")!;
    private Button H265ButtonControl => this.FindControl<Button>("H265Button")!;
    private Button CpuButtonControl => this.FindControl<Button>("CpuButton")!;
    private Button GpuButtonControl => this.FindControl<Button>("GpuButton")!;
    private ComboBox QualityComboControl => this.FindControl<ComboBox>("QualityCombo")!;
    private Button OutputDirButtonControl => this.FindControl<Button>("OutputDirButton")!;
    private TextBlock StatusTextControl => this.FindControl<TextBlock>("StatusText")!;
    private Button ExecuteButtonControl => this.FindControl<Button>("ExecuteButton")!;
    private TextBlock ExecuteTitleTextControl => this.FindControl<TextBlock>("ExecuteTitleText")!;
    private TextBlock ExecuteSubtitleTextControl => this.FindControl<TextBlock>("ExecuteSubtitleText")!;
    private Border AdOverlayControl => this.FindControl<Border>("AdOverlay")!;
    private ContentControl AdWebHostControl => this.FindControl<ContentControl>("AdWebHost")!;
    private TextBlock AdFallbackTextControl => this.FindControl<TextBlock>("AdFallbackText")!;
    private Control DropDashedBorderControl => this.FindControl<Control>("DropDashedBorder")!;
    private NativeWebView? adWebView;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        proActivated = LicenseManager.IsProActivated(licenseStore);
        UpdateProButton();
        RefreshOptionButtons();
        StatusTextControl.Text = OutputLocationText();

        DragDrop.SetAllowDrop(DropAreaControl, true);
        DropAreaControl.AddHandler(DragDrop.DragOverEvent, FilesDragOver);
        DropAreaControl.AddHandler(DragDrop.DropEvent, FilesDrop);

        Opened += async (_, _) =>
        {
            adConfig = await AdConfigManager.LoadRemoteOrDefaultAsync(CancellationToken.None).ConfigureAwait(true);
            await CheckToolsAtStartupAsync().ConfigureAwait(true);
        };

        adTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        adTimer.Tick += (_, _) => UpdateAdCountdown();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task CheckToolsAtStartupAsync()
    {
        try
        {
            ffmpegPath = runtimeResolver.ResolveFfmpegPath();
            ffprobePath = runtimeResolver.ResolveFfprobePath();

            if (!runtimeResolver.RuntimeExists())
            {
                var accepted = await SimpleDialog.ConfirmAsync(
                    this,
                    "安装 FFmpeg 组件",
                    "首次运行需要下载 FFmpeg 运行组件。\n\n点击“是”后将自动下载并安装。").ConfigureAwait(true);

                if (!accepted)
                {
                    toolsReady = false;
                    StatusTextControl.Text = "未安装 FFmpeg 组件";
                    SetHint("未安装 FFmpeg 组件，不能开始压缩。");
                    return;
                }

                var installer = new RuntimeInstaller(runtimeResolver);
                await installer.InstallAsync(new Progress<string>(SetHint), CancellationToken.None).ConfigureAwait(true);
                ffmpegPath = runtimeResolver.ResolveFfmpegPath();
                ffprobePath = runtimeResolver.ResolveFfprobePath();
            }

            encoderList = await RunCaptureAsync(ffmpegPath, ["-hide_banner", "-encoders"], TimeSpan.FromSeconds(12), CancellationToken.None).ConfigureAwait(true);
            toolsReady = HasEncoder("libx264") && HasEncoder("libx265");

            if (!toolsReady)
            {
                StatusTextControl.Text = "FFmpeg 缺少 libx264 或 libx265。";
                SetHint("当前 FFmpeg 缺少必要编码器，不能开始压缩。");
                return;
            }

            StatusTextControl.Text = OutputLocationText();
            SetHint(DefaultHintText());
        }
        catch (Exception ex)
        {
            toolsReady = false;
            StatusTextControl.Text = "FFmpeg 检查失败";
            SetHint("FFmpeg 检查失败：" + ex.Message);
        }
    }

    private void FilesDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void FilesDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files == null)
        {
            return;
        }

        AddFiles(files.Select(file => file.Path.LocalPath));
        e.Handled = true;
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        if (isRunning)
        {
            return;
        }

        var existing = Jobs.Select(job => job.InputPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (!File.Exists(path) ||
                !SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase) ||
                existing.Contains(path))
            {
                continue;
            }

            Jobs.Add(new VideoJob { InputPath = path });
            existing.Add(path);
        }

        RefreshJobView();
    }

    private void RefreshJobView()
    {
        var hasJobs = Jobs.Count > 0;
        JobScrollControl.IsVisible = hasJobs;
        HintTextControl.IsVisible = !hasJobs;
        if (!hasJobs)
        {
            SetHint(DefaultHintText());
        }
    }

    private void H264Button_Click(object? sender, RoutedEventArgs e)
    {
        if (isRunning)
        {
            return;
        }

        selectedCodec = CodecChoice.H264;
        RefreshOptionButtons();
    }

    private void H265Button_Click(object? sender, RoutedEventArgs e)
    {
        if (isRunning)
        {
            return;
        }

        selectedCodec = CodecChoice.H265;
        RefreshOptionButtons();
    }

    private void CpuButton_Click(object? sender, RoutedEventArgs e)
    {
        if (isRunning)
        {
            return;
        }

        selectedEngine = EngineChoice.Cpu;
        RefreshOptionButtons();
        SetHint(Jobs.Count == 0 ? DefaultHintText() : "");
    }

    private async void GpuButton_Click(object? sender, RoutedEventArgs e)
    {
        if (isRunning)
        {
            return;
        }

        selectedEngine = EngineChoice.Gpu;
        RefreshOptionButtons();

        var encoder = FfmpegArgumentBuilder.GetRequiredEncoder(selectedCodec, selectedEngine);
        if (!await IsGpuEncoderUsableAsync(encoder).ConfigureAwait(true))
        {
            SetHint("当前机器未检测到可用 Apple VideoToolbox 编码器，建议使用 CPU。");
            return;
        }

        SetHint(Jobs.Count == 0 ? "GPU 编码器可用，可以拖入视频开始压缩。" : "");
    }

    private void QualityCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
        {
            return;
        }

        selectedQuality = comboBox.SelectedIndex switch
        {
            0 => QualityChoice.High,
            2 => QualityChoice.Small,
            _ => QualityChoice.Balanced
        };
    }

    private async void OutputDirButton_Click(object? sender, RoutedEventArgs e)
    {
        if (isRunning)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择压缩后视频的保存目录",
            AllowMultiple = false
        }).ConfigureAwait(true);

        var folder = folders.FirstOrDefault();
        if (folder != null)
        {
            customOutputDirectory = folder.Path.LocalPath;
            StatusTextControl.Text = OutputLocationText();
        }
    }

    private async void ProButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new LicenseDialog(proActivated, licenseStore);
        var result = await dialog.ShowDialog<bool?>(this).ConfigureAwait(true);
        if (result == true || dialog.LicenseActivated)
        {
            proActivated = true;
            UpdateProButton();
        }
    }

    private void ExecuteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (queueFinished)
        {
            if (adGateActive)
            {
                StopAdGate();
            }

            ClearJobs();
            return;
        }

        if (isRunning)
        {
            return;
        }

        if (!toolsReady)
        {
            SetHint("FFmpeg 未通过检查，不能开始压缩。");
            return;
        }

        if (Jobs.Count == 0)
        {
            SetHint("请先拖入一个或多个视频文件。");
            return;
        }

        StartQueue();
    }

    private void StartQueue()
    {
        isRunning = true;
        queueFinished = false;
        queueWorkFinished = false;
        pendingFailures = 0;
        SetExecuteButtonState("执行中", ExecuteTitleRunningFontSize);
        SetControlsEnabled(false);
        SetHint("");
        StartAdGate();
        _ = Task.Run(RunQueueAsync);
    }

    private async Task RunQueueAsync()
    {
        var failures = 0;
        foreach (var job in Jobs)
        {
            if (job.Status == "完成")
            {
                continue;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                job.Status = "进行中";
                job.Progress = 0;
                job.ErrorMessage = "";
                job.OutputPath = OutputPathService.CreateOutputPath(job.InputPath, customOutputDirectory, selectedCodec);
            });

            job.DurationSeconds = await ffprobeRunner.ProbeDurationAsync(ffprobePath, job.InputPath, CancellationToken.None).ConfigureAwait(false);
            var options = new CompressionOptions(job.InputPath, job.OutputPath, selectedCodec, selectedEngine, selectedQuality);
            var ok = await ffmpegRunner.RunAsync(
                ffmpegPath,
                job,
                options,
                progress => Dispatcher.UIThread.Post(() => job.Progress = Math.Max(job.Progress, progress)),
                CancellationToken.None).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ok)
                {
                    job.Progress = 100;
                    job.Status = "完成";
                }
                else
                {
                    failures++;
                    job.Progress = 0;
                    job.Status = "失败";
                }
            });
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MarkQueueFinished(failures);
            if (adGateActive && AdOverlayControl.IsVisible)
            {
                SetHint("");
                return;
            }
        });
    }

    private void StartAdGate()
    {
        if (proActivated)
        {
            adGateActive = false;
            return;
        }

        currentAd = AdConfigManager.SelectAd(adConfig, MacPlatformInfo.PlatformName, AppVersion, null);
        adGateState = AdGateState.Start(currentAd, DateTime.UtcNow);
        adMuted = DefaultAdMuted;
        adGateActive = true;
        JobScrollControl.IsVisible = false;
        HintTextControl.IsVisible = false;
        AdFallbackTextControl.IsVisible = false;
        AdOverlayControl.IsVisible = true;
        DropDashedBorderControl.IsVisible = false;
        UpdateAdCountdown();

        try
        {
            var webView = EnsureAdWebView();
            AdWebHostControl.Content = webView;
            webView.NavigateToString(AdHtmlBuilder.Build(currentAd.VideoUrl, adMuted), new Uri(AdConfigManager.DefaultPurchaseUrl));
            UpdateAdCountdown();
        }
        catch (Exception ex)
        {
            AdFallbackTextControl.Text = "广告视频暂不可用\n" + ex.Message;
            AdFallbackTextControl.IsVisible = true;
        }

        adTimer.Start();
    }

    private void UpdateAdCountdown()
    {
        if (!adGateActive || adGateState == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        UpdateAdHtmlState(adGateState.CountdownText(now), adGateState.CanClose(now));

        if (queueWorkFinished && adGateState.TakeMinimumReached(now))
        {
            StatusTextControl.Text = CompletionStatusText(pendingFailures);
        }
    }

    private bool CanCloseAd()
    {
        return adGateState?.CanClose(DateTime.UtcNow) == true;
    }

    private void AdWebView_WebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        switch (NormalizeWebMessage(e.Body))
        {
            case "details":
                OpenCurrentAdDetails();
                break;
            case "close":
                CloseAdIfAllowed();
                break;
        }
    }

    private void OpenCurrentAdDetails()
    {
        try
        {
            var url = AdConfigManager.BuildTrackedClickUrl(currentAd, AppVersion, "compress_waiting", MacPlatformInfo.PlatformName);
            MacBrowserLauncher.OpenUrl(url);
        }
        catch
        {
            SetHint("无法打开广告详情链接。");
        }
    }

    private void CloseAdIfAllowed()
    {
        if (!CanCloseAd())
        {
            return;
        }

        StopAdGate();
        if (queueWorkFinished)
        {
            StatusTextControl.Text = CompletionStatusText(pendingFailures);
        }
    }

    private void StopAdGate()
    {
        adTimer.Stop();
        adGateActive = false;
        adGateState = null;
        AdOverlayControl.IsVisible = false;
        DropDashedBorderControl.IsVisible = true;
        try
        {
            adWebView?.Stop();
            adWebView?.NavigateToString("<html><body></body></html>", new Uri(AdConfigManager.DefaultPurchaseUrl));
        }
        catch
        {
        }

        AdWebHostControl.Content = null;
        RefreshJobView();
    }

    private NativeWebView EnsureAdWebView()
    {
        if (adWebView != null)
        {
            return adWebView;
        }

        var webView = new NativeWebView();
        webView.EnvironmentRequested += AdWebView_EnvironmentRequested;
        webView.WebMessageReceived += AdWebView_WebMessageReceived;
        adWebView = webView;
        return webView;
    }

    private static void AdWebView_EnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        if (e is AppleWKWebViewEnvironmentRequestedEventArgs apple)
        {
            apple.ScriptHandlerMessageName = AdMessageHandlerName;
        }
    }

    private void UpdateAdHtmlState(string countdownText, bool canClose)
    {
        var textLiteral = JsonSerializer.Serialize(countdownText);
        var canCloseLiteral = canClose ? "true" : "false";
        InvokeAdScript("window.setYbbAdState && window.setYbbAdState(" + textLiteral + ", " + canCloseLiteral + ");");
    }

    private void InvokeAdScript(string script)
    {
        var webView = adWebView;
        if (webView == null)
        {
            return;
        }

        _ = InvokeAdScriptAsync(webView, script);
    }

    private static async Task InvokeAdScriptAsync(NativeWebView webView, string script)
    {
        try
        {
            await webView.InvokeScript(script).ConfigureAwait(true);
        }
        catch
        {
        }
    }

    private static string NormalizeWebMessage(string? body)
    {
        var message = (body ?? "").Trim();
        if (message.Length >= 2 && message[0] == '"' && message[^1] == '"')
        {
            try
            {
                return JsonSerializer.Deserialize<string>(message) ?? message;
            }
            catch
            {
            }
        }

        return message;
    }

    private void MarkQueueFinished(int failures)
    {
        isRunning = false;
        queueFinished = true;
        queueWorkFinished = true;
        pendingFailures = failures;
        SetExecuteButtonState("完成", "点击返回");
        StatusTextControl.Text = CompletionStatusText(failures);
        SetHint("");
    }

    private void ClearJobs()
    {
        Jobs.Clear();
        queueFinished = false;
        queueWorkFinished = false;
        pendingFailures = 0;
        SetControlsEnabled(true);
        SetExecuteButtonState("执行");
        StatusTextControl.Text = OutputLocationText();
        RefreshJobView();
    }

    private void SetExecuteButtonState(string title, string subtitle = "")
    {
        SetExecuteButtonState(title, ExecuteTitleNormalFontSize, subtitle);
    }

    private void SetExecuteButtonState(string title, double titleFontSize, string subtitle = "")
    {
        ExecuteTitleTextControl.Text = title;
        ExecuteTitleTextControl.FontSize = titleFontSize;
        ExecuteSubtitleTextControl.Text = subtitle;
        ExecuteSubtitleTextControl.IsVisible = !string.IsNullOrWhiteSpace(subtitle);
    }

    private static string CompletionStatusText(int failures)
    {
        return failures == 0 ? "压缩已完成，点击返回" : "部分失败，点击返回";
    }

    private async Task<bool> IsGpuEncoderUsableAsync(string encoder)
    {
        if (!HasEncoder(encoder))
        {
            return false;
        }

        try
        {
            var args = new[]
            {
                "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "testsrc2=size=64x64:rate=1",
                "-t", "0.1", "-an", "-c:v", encoder, "-f", "null", "-"
            };
            await RunCaptureAsync(ffmpegPath, args, TimeSpan.FromSeconds(10), CancellationToken.None).ConfigureAwait(true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool HasEncoder(string encoder)
    {
        return encoderList.IndexOf(" " + encoder + " ", StringComparison.OrdinalIgnoreCase) >= 0 ||
               encoderList.IndexOf(" " + encoder + "\n", StringComparison.OrdinalIgnoreCase) >= 0 ||
               encoderList.IndexOf(" " + encoder + "\r", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static async Task<string> RunCaptureAsync(string fileName, IEnumerable<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动命令。");
        var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
        await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        var output = await outputTask.ConfigureAwait(false) + "\n" + await errorTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(output);
        }

        return output;
    }

    private void RefreshOptionButtons()
    {
        SetButtonSelected(H264ButtonControl, selectedCodec == CodecChoice.H264);
        SetButtonSelected(H265ButtonControl, selectedCodec == CodecChoice.H265);
        SetButtonSelected(CpuButtonControl, selectedEngine == EngineChoice.Cpu);
        SetButtonSelected(GpuButtonControl, selectedEngine == EngineChoice.Gpu);
    }

    private static void SetButtonSelected(Button button, bool selected)
    {
        button.Background = selected ? Brushes.Black : new SolidColorBrush(Color.FromRgb(214, 214, 214));
        button.Foreground = selected ? Brushes.White : new SolidColorBrush(Color.FromRgb(55, 55, 55));
    }

    private void SetControlsEnabled(bool enabled)
    {
        H264ButtonControl.IsEnabled = enabled;
        H265ButtonControl.IsEnabled = enabled;
        CpuButtonControl.IsEnabled = enabled;
        GpuButtonControl.IsEnabled = enabled;
        QualityComboControl.IsEnabled = enabled;
        OutputDirButtonControl.IsEnabled = enabled;
    }

    private void UpdateProButton()
    {
        ProButtonControl.Content = CreateButtonLabel(proActivated ? "Pro 已激活" : "升级 Pro");
    }

    private static TextBlock CreateButtonLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { "ButtonLabel" }
        };
    }

    private string OutputLocationText()
    {
        return string.IsNullOrWhiteSpace(customOutputDirectory)
            ? "保存到：源文件旁"
            : "保存到：" + customOutputDirectory;
    }

    private static string DefaultHintText()
    {
        return "拖入视频文件，进行压缩。\n压缩完成后，文件将保存在源文件旁。";
    }

    private void SetHint(string text)
    {
        HintTextControl.Text = text;
        HintTextControl.IsVisible = !JobScrollControl.IsVisible || !string.IsNullOrWhiteSpace(text);
    }
}
