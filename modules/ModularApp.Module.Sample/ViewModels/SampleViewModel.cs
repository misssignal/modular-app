using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModularApp.Ui.ViewModels;

namespace ModularApp.Module.Sample.ViewModels;

public partial class SampleViewModel : ModuleViewModelBase
{
    [ObservableProperty]
    private string _title = "Sample Module";

    [ObservableProperty]
    private string _configInfo = "";

    [RelayCommand]
    private void LoadConfig()
    {
        var displayMode = CoreServices.Configuration.GetValue("DisplayMode") ?? "default";
        var maxResults = CoreServices.Configuration.GetValue("MaxResults") ?? "N/A";
        ConfigInfo = $"Display Mode: {displayMode}, Max Results: {maxResults}";
    }
}
