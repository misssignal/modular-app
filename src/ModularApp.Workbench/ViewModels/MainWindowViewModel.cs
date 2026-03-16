using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using ModularApp.Workbench.Services;
using ModularApp.Workbench.Views;

namespace ModularApp.Workbench.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IModuleLoader _loader;
    private readonly IModuleRegistry _registry;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ModuleNavItem> _modules = new();

    [ObservableProperty]
    private ModuleNavItem? _selectedModule;

    [ObservableProperty]
    private object? _activeModuleContent;

    [ObservableProperty]
    private string _statusText = "Ready";

    public MainWindowViewModel(
        IModuleLoader loader,
        IModuleRegistry registry,
        ILogger<MainWindowViewModel> logger)
    {
        _loader = loader;
        _registry = registry;
        _logger = logger;
    }

    public async Task LoadModulesAsync()
    {
        try
        {
            StatusText = "Discovering modules...";
            var discovered = await _loader.DiscoverModulesAsync();

            foreach (var result in discovered)
            {
                _registry.Register(result);
                Modules.Add(new ModuleNavItem(result.ModuleId, result.ModuleName, result.IconKey));
                _logger.LogInformation("Discovered module: {ModuleId} v{Version}", result.ModuleId, result.ModuleVersion);
            }

            StatusText = $"{Modules.Count} module(s) loaded";
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
            ActiveModuleContent = null;
            return;
        }

        _ = ActivateModuleAsync(value);
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
