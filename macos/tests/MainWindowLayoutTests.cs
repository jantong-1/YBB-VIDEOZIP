using System.Globalization;
using System.Xml.Linq;

namespace YBBvideozip.Mac.Tests;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void MainWindowKeepsSeventyPercentLayoutWithOriginalFontSizes()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        Assert.Equal("637", Attr(root, "Width"));
        Assert.Equal("480", Attr(root, "Height"));
        Assert.Equal("637", Attr(root, "MinWidth"));
        Assert.Equal("480", Attr(root, "MinHeight"));
        Assert.Equal("False", Attr(root, "CanResize"));

        AssertControl(root, "DropArea", "17", "17", "603", "339.1875");
        AssertControl(root, "H264Button", "24", "374", "84", "34");
        AssertControl(root, "H265Button", "116", "374", "84", "34");
        AssertControl(root, "CpuButton", "223", "374", "84", "34");
        AssertControl(root, "GpuButton", "315", "374", "84", "34");
        AssertControl(root, "QualityCombo", "24", "420", "176", "34");
        AssertControl(root, "OutputDirButton", "223", "420", "84", "34");
        AssertControl(root, "StatusTextBox", "315", "420", "179", "34");
        AssertControl(root, "ExecuteButton", "512", "374", "90", "76");

        Assert.Equal("25", Attr(Named(root, "HintText"), "FontSize"));
        Assert.Equal("23", Attr(Named(root, "H264Button"), "FontSize"));
        Assert.Equal("23", Attr(Named(root, "H265Button"), "FontSize"));
        Assert.Equal("23", Attr(Named(root, "CpuButton"), "FontSize"));
        Assert.Equal("23", Attr(Named(root, "GpuButton"), "FontSize"));
        Assert.Equal("16", Attr(Named(root, "QualityCombo"), "FontSize"));
        Assert.Equal("14", Attr(Named(root, "OutputDirButton"), "FontSize"));
        Assert.Equal("30", Attr(Named(root, "ExecuteTitleText"), "FontSize"));
        Assert.Equal("11", Attr(Named(root, "ExecuteSubtitleText"), "FontSize"));
        Assert.Equal("False", Attr(Named(root, "ExecuteSubtitleText"), "IsVisible"));
    }

    [Fact]
    public void DropAreaKeepsSixteenByNineVideoContainer()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");
        var dropArea = Named(root, "DropArea");

        var width = double.Parse(Attr(dropArea, "Width"), CultureInfo.InvariantCulture);
        var height = double.Parse(Attr(dropArea, "Height"), CultureInfo.InvariantCulture);

        Assert.Equal(16.0 / 9.0, width / height, precision: 4);
    }

    [Fact]
    public void DropAreaDoesNotPaintSquareBackgroundOutsideRoundedDashedBorder()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        Assert.Equal("Transparent", Attr(Named(root, "DropArea"), "Background"));

        var roundedFill = root.Descendants()
            .Single(node => node.Name.LocalName == "Rectangle" && AttrOrNull(node, "Fill") == "#DADADA");
        Assert.Equal("15", Attr(roundedFill, "RadiusX"));
        Assert.Equal("15", Attr(roundedFill, "RadiusY"));
    }

    [Fact]
    public void JobProgressBarIsConstrainedToProgressColumn()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        var progressBar = root.Descendants().Single(node => node.Name.LocalName == "ProgressBar");

        Assert.Equal("0", Attr(progressBar, "MinWidth"));
        Assert.Equal("Stretch", Attr(progressBar, "HorizontalAlignment"));
        Assert.Equal("True", Attr(progressBar, "ClipToBounds"));
    }

    [Fact]
    public void AdOverlayIsRoundedClippedLayerBelowDashedBorder()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        var dropArea = Named(root, "DropArea");
        var adOverlay = Named(root, "AdOverlay");
        var dashedBorder = Named(root, "DropDashedBorder");

        Assert.Equal("Border", adOverlay.Name.LocalName);
        Assert.Equal("0", Attr(adOverlay, "Margin"));
        Assert.Equal("Transparent", Attr(adOverlay, "Background"));
        Assert.Equal("True", Attr(adOverlay, "ClipToBounds"));

        Assert.Equal("Transparent", Attr(dashedBorder, "Fill"));
        Assert.Equal("False", Attr(dashedBorder, "IsHitTestVisible"));
        Assert.Same(dashedBorder, dropArea.Elements().Last());
    }

    [Fact]
    public void AdWebHostFillsAdOverlaySoVideoMatchesDashedFrame()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        var adWebHost = Named(root, "AdWebHost");

        Assert.Equal("0", Attr(adWebHost, "Margin"));
        Assert.Equal("Stretch", Attr(adWebHost, "HorizontalAlignment"));
        Assert.Equal("Stretch", Attr(adWebHost, "VerticalAlignment"));
        Assert.Null(AttrOrNull(adWebHost, "Grid.Row"));
        Assert.Null(AttrOrNull(adWebHost, "Grid.RowSpan"));
    }

    [Fact]
    public void AdOverlayControlsLiveInsideHtmlSoNativeLayerDoesNotCoverThem()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        var adOverlay = Named(root, "AdOverlay");

        Assert.DoesNotContain(adOverlay.Descendants(), node => node.Name.LocalName == "Button");
        Assert.DoesNotContain(adOverlay.Descendants(), node =>
            AttrOrNull(node, "Name") is "AdSoundButton" or "AdClickButton" or "AdCloseButton" or "AdCountdownText");
    }

    [Fact]
    public void AdOverlayControlsDoNotUseButtonHoverVisuals()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");
        var adOverlay = Named(root, "AdOverlay");

        Assert.DoesNotContain(adOverlay.Descendants(), node => node.Name.LocalName == "Button");
        Assert.DoesNotContain(adOverlay.Descendants(), node => AttrOrNull(node, "Name")?.StartsWith("Ad") == true && node.Name.LocalName == "Button");
    }

    [Fact]
    public void StatusTextIsVerticallyCenteredBesideOutputButton()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        var statusBox = Named(root, "StatusTextBox");
        var statusText = Named(root, "StatusText");

        Assert.Equal("Center", Attr(statusBox, "VerticalAlignment"));
        Assert.Equal("Center", Attr(statusText, "VerticalAlignment"));
    }

    [Fact]
    public void MainActionButtonLabelsUseMacVerticalCenteringClass()
    {
        var document = XDocument.Load(FindSourceFile("MainWindow.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        foreach (var name in new[] { "H264Button", "H265Button", "CpuButton", "GpuButton", "OutputDirButton" })
        {
            Assert.Equal("YbbButton", Attr(Named(root, name), "Classes"));
            var label = Named(root, name).Descendants().Single(node => node.Name.LocalName == "TextBlock");
            Assert.Equal("ButtonLabel", Attr(label, "Classes"));
        }

        Assert.Equal("YbbButton", Attr(Named(root, "ExecuteButton"), "Classes"));
        Assert.Equal("ButtonLabel ExecuteTitle", Attr(Named(root, "ExecuteTitleText"), "Classes"));
    }

    [Fact]
    public void AppDefinesMacButtonLabelStyleWithLineHeightAndVisualOffset()
    {
        var document = XDocument.Load(FindSourceFile("App.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        var style = root.Descendants().Single(node => AttrOrNull(node, "Selector") == "TextBlock.ButtonLabel");
        var setters = style.Elements().Where(node => node.Name.LocalName == "Setter").ToDictionary(node => Attr(node, "Property"), node => Attr(node, "Value"));

        Assert.Equal("Center", setters["VerticalAlignment"]);
        Assert.Equal("Center", setters["HorizontalAlignment"]);
        Assert.Equal("0,2,0,0", setters["Margin"]);
        Assert.Equal("34", setters["LineHeight"]);
    }

    [Fact]
    public void MainActionButtonsUseCustomTemplateInsteadOfDefaultMacButtonChrome()
    {
        var document = XDocument.Load(FindSourceFile("App.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");

        var style = root.Descendants().Single(node => AttrOrNull(node, "Selector") == "Button.YbbButton");

        Assert.Contains(style.Descendants(), node => node.Name.LocalName == "ControlTemplate");
        Assert.Contains(style.Descendants(), node => node.Name.LocalName == "Border");
        Assert.Contains(style.Descendants(), node => node.Name.LocalName == "ContentPresenter");
    }

    [Fact]
    public void LicenseDialogButtonsUseMacVerticalCenteringClass()
    {
        var document = XDocument.Load(FindSourceFile("LicenseDialog.axaml"));
        var root = document.Root ?? throw new InvalidOperationException("Missing XAML root.");
        var buttons = root.Descendants().Where(node => node.Name.LocalName == "Button").ToArray();

        Assert.Equal(2, buttons.Length);
        foreach (var button in buttons)
        {
            Assert.Equal("YbbButton", Attr(button, "Classes"));
            var label = button.Descendants().Single(node => node.Name.LocalName == "TextBlock");
            Assert.Equal("ButtonLabel", Attr(label, "Classes"));
        }
    }

    private static void AssertControl(XContainer root, string name, string left, string top, string width, string height)
    {
        var element = Named(root, name);

        Assert.Equal(left, Attr(element, "Canvas.Left"));
        Assert.Equal(top, Attr(element, "Canvas.Top"));
        Assert.Equal(width, Attr(element, "Width"));
        Assert.Equal(height, Attr(element, "Height"));
    }

    private static XElement Named(XContainer root, string name)
    {
        return root.Descendants().Single(node => AttrOrNull(node, "Name") == name);
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

            candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find source XAML.", fileName);
    }

    private static string Attr(XElement element, string localName)
    {
        return AttrOrNull(element, localName)
            ?? throw new InvalidOperationException("Missing attribute " + localName + " on " + element.Name.LocalName + ".");
    }

    private static string? AttrOrNull(XElement element, string localName)
    {
        return element.Attributes().FirstOrDefault(attr => attr.Name.LocalName == localName)?.Value;
    }
}
