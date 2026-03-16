using Avalonia.Controls;
using Avalonia.Input;

namespace ModularApp.Workbench.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Hidden reset shortcut: Ctrl+Shift+Alt+F12
        if (e.Key == Key.F12 &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            e.Handled = true;
            _ = ResetSetupAsync();
        }
    }

    private async Task ResetSetupAsync()
    {
        var dialog = new Window
        {
            Title = "Reset Setup",
            Width = 380,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var result = false;

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 16,
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Reset setup and return to first-time configuration?\nThis will delete your saved profile.",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
        };

        var cancelButton = new Button { Content = "Cancel" };
        cancelButton.Click += (_, _) => dialog.Close();

        var resetButton = new Button { Content = "Reset", Classes = { "accent" } };
        resetButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(resetButton);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;
        await dialog.ShowDialog(this);

        if (result)
        {
            var profileStore = new Services.SetupProfileStore(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.SetupProfileStore>.Instance);
            profileStore.Delete();

            // Swap back to setup view
            if (Avalonia.Application.Current is App app)
            {
                DataContext = null;
                app.ShowSetupView(this);
            }
        }
    }
}
