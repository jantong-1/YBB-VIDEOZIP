using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using YBBvideozip.Mac.Ads;
using YBBvideozip.Mac.Licensing;
using YBBvideozip.Mac.Platform;

namespace YBBvideozip.Mac;

public sealed partial class LicenseDialog : Window
{
    private readonly LicenseStore store = new();

    public bool LicenseActivated { get; private set; }

    private TextBox LicenseTextBoxControl => this.FindControl<TextBox>("LicenseTextBox")!;
    private TextBlock StatusTextControl => this.FindControl<TextBlock>("StatusText")!;

    public LicenseDialog()
        : this(false, new LicenseStore())
    {
    }

    public LicenseDialog(bool alreadyActivated, LicenseStore store)
    {
        this.store = store;
        InitializeComponent();
        StatusTextControl.Text = alreadyActivated ? "当前已激活 Pro。" : "付款后输入授权码即可去广告。";
        StatusTextControl.Foreground = alreadyActivated ? new SolidColorBrush(Color.FromRgb(40, 120, 70)) : new SolidColorBrush(Color.FromRgb(120, 120, 120));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ActivateButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (LicenseManager.SaveLicenseCode(LicenseTextBoxControl.Text ?? "", store, out var message))
        {
            LicenseActivated = true;
            StatusTextControl.Foreground = new SolidColorBrush(Color.FromRgb(40, 120, 70));
            StatusTextControl.Text = message;
            Close(true);
            return;
        }

        StatusTextControl.Foreground = new SolidColorBrush(Color.FromRgb(170, 40, 40));
        StatusTextControl.Text = message;
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private void PurchaseLink_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            MacBrowserLauncher.OpenUrl(AdConfigManager.DefaultPurchaseUrl);
        }
        catch
        {
            StatusTextControl.Foreground = new SolidColorBrush(Color.FromRgb(170, 40, 40));
            StatusTextControl.Text = "无法打开购买页面，请手动访问上方链接。";
        }
    }
}
