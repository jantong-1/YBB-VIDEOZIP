using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace YBBvideozip.Mac.Controls;

public static class SimpleDialog
{
    public static async Task<bool> ConfirmAsync(Window owner, string title, string message)
    {
        var result = false;
        var window = CreateBaseWindow(title, message);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 10
        };

        var okButton = CreateDialogButton("是");
        okButton.Click += (_, _) =>
        {
            result = true;
            window.Close();
        };

        var cancelButton = CreateDialogButton("否");
        cancelButton.Click += (_, _) => window.Close();

        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        ((StackPanel)window.Content!).Children.Add(buttons);

        await window.ShowDialog(owner).ConfigureAwait(true);
        return result;
    }

    public static async Task AlertAsync(Window owner, string title, string message)
    {
        var window = CreateBaseWindow(title, message);
        var button = new Button
        {
            Content = CreateButtonLabel("确定"),
            MinWidth = 86,
            HorizontalAlignment = HorizontalAlignment.Right,
            Classes = { "YbbButton" }
        };
        button.Click += (_, _) => window.Close();
        ((StackPanel)window.Content!).Children.Add(button);
        await window.ShowDialog(owner).ConfigureAwait(true);
    }

    private static Window CreateBaseWindow(string title, string message)
    {
        return new Window
        {
            Title = title,
            Width = 420,
            Height = 210,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brushes.White,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 15,
                        Foreground = Brushes.Black
                    }
                }
            }
        };
    }

    private static Button CreateDialogButton(string text)
    {
        return new Button
        {
            Width = 76,
            Height = 32,
            Padding = new Avalonia.Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = CreateButtonLabel(text),
            Classes = { "YbbButton" }
        };
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
}
