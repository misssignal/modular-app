using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ModularApp.Sdk;
using ModularApp.Workbench.Services;
using ModularApp.Workbench.Views;

namespace ModularApp.Workbench.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IModuleLoader _loader;
    private readonly IModuleRegistry _registry;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<MainWindowViewModel> _logger;

    private SetupProfile? _profile;
    private ShellSettingsViewModel? _settingsVm;
    private ShellSettingsView? _settingsView;
    private AdminViewModel? _adminVm;
    private AdminView? _adminView;

    [ObservableProperty]
    private ObservableCollection<ModuleNavItem> _modules = new();

    [ObservableProperty]
    private ModuleNavItem? _selectedModule;

    [ObservableProperty]
    private object? _activeModuleContent;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isSettingsActive;

    [ObservableProperty]
    private bool _isAdminVisible;

    public MainWindowViewModel(
        IModuleLoader loader,
        IModuleRegistry registry,
        IPermissionService permissionService,
        ILogger<MainWindowViewModel> logger)
    {
        _loader = loader;
        _registry = registry;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>Set the setup profile for module discovery configuration.</summary>
    public void SetProfile(SetupProfile profile)
    {
        _profile = profile;
    }

    /// <summary>Set the shell settings ViewModel for the gear icon panel.</summary>
    public void SetSettingsViewModel(ShellSettingsViewModel settingsVm)
    {
        _settingsVm = settingsVm;
    }

    /// <summary>Set the admin ViewModel and determine visibility.</summary>
    public void SetAdminViewModel(AdminViewModel adminVm)
    {
        _adminVm = adminVm;
        _adminVm.Refresh();
        IsAdminVisible = _adminVm.AdminLabs.Count > 0;
    }

    public async Task LoadModulesAsync()
    {
        try
        {
            StatusText = "Discovering modules...";
            var discovered = await _loader.DiscoverModulesAsync();
            var userId = _profile?.Username ?? Environment.UserName;
            var activeLabs = _profile?.SelectedLabs.AsReadOnly()
                ?? (IReadOnlyList<string>)Array.Empty<string>();

            var registered = 0;
            var hidden = 0;

            foreach (var result in discovered)
            {
                // Permission gate: skip modules the user can't see
                var effectivePermission = _permissionService.GetEffectivePermission(
                    userId, activeLabs, result.ModuleId);

                if (effectivePermission == PermissionLevel.Hidden)
                {
                    _logger.LogDebug("Module {ModuleId} hidden for user {UserId} in labs [{Labs}]",
                        result.ModuleId, userId, string.Join(", ", activeLabs));
                    hidden++;
                    continue;
                }

                _registry.Register(result);
                Modules.Add(new ModuleNavItem(
                    result.ModuleId, result.ModuleName, result.IconKey, effectivePermission));
                _logger.LogInformation(
                    "Discovered module: {ModuleId} v{Version} (permission: {Level})",
                    result.ModuleId, result.ModuleVersion, effectivePermission);
                registered++;
            }

            StatusText = hidden > 0
                ? $"{registered} module(s) loaded ({hidden} hidden)"
                : $"{registered} module(s) loaded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover modules");
            StatusText = "Error loading modules";
        }
    }

    partial void OnSelectedModuleChanged(ModuleNavItem? value)
    {
        if (value is null)
        {
            if (!IsSettingsActive)
                ActiveModuleContent = null;
            return;
        }

        // Deselect settings when a module is selected
        IsSettingsActive = false;
        _ = ActivateModuleAsync(value);
    }

    [RelayCommand]
    private void OpenAdmin()
    {
        SelectedModule = null;
        IsSettingsActive = false;

        if (_adminVm is not null)
        {
            _adminVm.Refresh();
            _adminView ??= new AdminView { DataContext = _adminVm };
            ActiveModuleContent = _adminView;
            StatusText = "Administration";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SelectedModule = null;
        IsSettingsActive = true;

        if (_settingsVm is not null)
        {
            _settingsVm.Refresh(_profile);
            _settingsView ??= new ShellSettingsView { DataContext = _settingsVm };
            ActiveModuleContent = _settingsView;
            StatusText = "Settings";
        }
    }

    private async Task ActivateModuleAsync(ModuleNavItem navItem)
    {
        try
        {
            StatusText = $"Loading {navItem.Name}...";
            var host = _registry.GetHost(navItem.Id);

            if (host is null)
            {
                ActiveModuleContent = new ModuleErrorView(navItem.Name, "Module not found in registry.");
                StatusText = $"{navItem.Name} - not found";
                return;
            }

            var view = await host.ActivateAsync();
            ActiveModuleContent = view;
            StatusText = navItem.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate module {ModuleId}", navItem.Id);
            ActiveModuleContent = new ModuleErrorView(navItem.Name, ex);
            StatusText = $"{navItem.Name} - error";
        }
    }

    /// <summary>Called by NavigationService to switch to a module by Id.</summary>
    public void NavigateToModule(string moduleId)
    {
        var item = Modules.FirstOrDefault(m => m.Id == moduleId);
        if (item is not null)
        {
            SelectedModule = item;
        }
    }

    /// <summary>Called by ModuleErrorView retry button.</summary>
    public async Task RetryModuleAsync(string moduleName)
    {
        var navItem = Modules.FirstOrDefault(m => m.Name == moduleName);
        if (navItem is null) return;

        var host = _registry.GetHost(navItem.Id);
        if (host is not null)
        {
            await host.DeactivateAsync();
            SelectedModule = null;
            SelectedModule = navItem;
        }
    }
}
