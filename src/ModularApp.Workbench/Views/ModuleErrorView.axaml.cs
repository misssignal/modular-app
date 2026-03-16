using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ModularApp.Workbench.ViewModels;

namespace ModularApp.Workbench.Views;

public partial class ModuleErrorView : UserControl
{
    private readonly string _moduleName;

    public ModuleErrorView()
    {
        InitializeComponent();
        _moduleName = "";
    }

    public ModuleErrorView(string moduleName, Exception ex)
    {
        InitializeComponent();
        _moduleName = moduleName;

        TitleText.Text = $"Module Failed: {moduleName}";
        MessageText.Text = ex.Message;
        StackTraceText.Text = ex.StackTrace;
        RetryButton.Click += OnRetryClick;
    }

    public ModuleErrorView(string moduleName, string errorMessage)
    {
        InitializeComponent();
        _moduleName = moduleName;

        TitleText.Text = $"Module Failed: {moduleName}";
        MessageText.Text = errorMessage;
        StackTraceText.Text = "";
        RetryButton.Click += OnRetryClick;
    }

    private void OnRetryClick(object? sender, RoutedEventArgs e)
    {
        // Walk up to find the MainWindow and its DataContext
        var window = this.FindAncestorOfType<MainWindow>();
        if (window?.DataContext is MainWindowViewModel vm)
        {
            _ = vm.RetryModuleAsync(_moduleName);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
