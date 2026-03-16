using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ModularApp.Workbench.Services;

namespace ModularApp.Workbench.ViewModels;

public partial class ShellSettingsViewModel : ViewModelBase
{
    private readonly SetupProfileStore _profileStore;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ILogger<ShellSettingsViewModel> _logger;

    // Profile section
    [ObservableProperty]
    private string _benchName = string.Empty;

    [ObservableProperty]
    private string _labArea = string.Empty;

    [ObservableProperty]
    private string _moduleSourceUri = string.Empty;

    [ObservableProperty]
    private string _tokenMasked = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _hostname = string.Empty;

    // Logging section
    [ObservableProperty]
    private string _selectedLogLevel = "Information";

    // About section
    [ObservableProperty]
    private string _workbenchVersion = "1.0.0";

    [ObservableProperty]
    private string _engineVersion = "1.0.0";

    // Status
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<ModuleInfoItem> LoadedModules { get; } = new();
    public string[] LogLevels { get; } = ["Trace", "Debug", "Information", "Warning", "Error"];

    /// <summary>Raised when profile changes require module re-discovery.</summary>
    public event Action? ModuleSourceChanged;

    public ShellSettingsViewModel(
        SetupProfileStore profileStore,
        IModuleRegistry moduleRegistry,
        ILogger<ShellSettingsViewModel> logger)
    {
        _profileStore = profileStore;
        _moduleRegistry = moduleRegistry;
        _logger = logger;
    }

    /// <summary>Load current profile and module info for display.</summary>
    public void Refresh(SetupProfile? profile)
    {
        if (profile is not null)
        {
            BenchName = profile.BenchName;
            LabArea = profile.LabArea;
            ModuleSourceUri = profile.ModuleSourceUri;
            TokenMasked = profile.Token.Length > 4
                ? $"****{profile.Token[^4..]}"
                : "****";
            Username = profile.Username;
            Hostname = profile.Hostname;
        }

        RefreshModuleList();
    }

    private void RefreshModuleList()
    {
        LoadedModules.Clear();

        foreach (var host in _moduleRegistry.GetAllHosts())
        {
            LoadedModules.Add(new ModuleInfoItem(
                host.ModuleId,
                host.ModuleName,
                host.ModuleVersion,
                host.State.ToString()));
        }
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var existing = _profileStore.Load();
        if (existing is null) return;

        var sourceChanged = existing.ModuleSourceUri != ModuleSourceUri;

        existing.BenchName = BenchName;

        _profileStore.Save(existing);
        StatusMessage = "Profile saved.";
        _logger.LogInformation("Profile updated: bench={BenchName}, labs=[{Labs}]",
            BenchName, string.Join(", ", existing.SelectedLabs));

        if (sourceChanged)
        {
            ModuleSourceChanged?.Invoke();
        }
    }
}

public record ModuleInfoItem(string Id, string Name, string Version, string Status);
