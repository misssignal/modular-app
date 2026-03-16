using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModularApp.Module.Sample.Views;

public partial class SampleView : UserControl
{
    public SampleView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
