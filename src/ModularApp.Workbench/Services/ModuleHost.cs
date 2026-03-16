using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

public enum ModuleHostState
{
    Discovered,
    Initialized,
    Active,
    Deactivated,
    Faulted
}

/// <summary>
/// Manages the lifecycle of a single module: instantiation, initialization,
/// view creation, and shutdown. Caches the view across navigation switches.
/// </summary>
public class ModuleHost
{
    private readonly ModuleDiscoveryResult _discovery;
    private readonly ICoreServicesFactory _coreServicesFactory;
    private readonly ILogger _logger;
    private IModule? _instance;
    private Control? _cachedView;

    public ModuleHostState State { get; private set; } = ModuleHostState.Discovered;
    public string ModuleId => _discovery.ModuleId;
    public string ModuleName => _discovery.ModuleName;
    public string ModuleVersion => _discovery.ModuleVersion;

    public ModuleHost(
        ModuleDiscoveryResult discovery,
        ICoreServicesFactory coreServicesFactory,
        ILogger logger)
    {
        _discovery = discovery;
        _coreServicesFactory = coreServicesFactory;
        _logger = logger;
    }

    public async Task<Control> ActivateAsync()
    {
        if (_cachedView is not null)
            return _cachedView;

        if (_instance is null)
        {
            var assembly = _discovery.LoadContext.LoadFromAssemblyPath(_discovery.DllPath);
            var type = assembly.GetType(_discovery.ModuleTypeName)
                ?? throw new InvalidOperationException($"Type {_discovery.ModuleTypeName} not found in {_discovery.DllPath}");

            _instance = (IModule)Activator.CreateInstance(type)!;

            var services = _coreServicesFactory.CreateForModule(_discovery.ModuleId, _discovery.ModuleDirectory);
            await _instance.InitializeAsync(services);
            State = ModuleHostState.Initialized;
            _logger.LogInformation("Module {ModuleId} initialized", _discovery.ModuleId);
        }

        // CreateView must run on the UI thread
        _cachedView = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var view = _instance.CreateView();
            if (view is not Control control)
                throw new InvalidOperationException(
                    $"Module {_discovery.ModuleId} CreateView() returned {view.GetType().Name}, expected Avalonia Control");
            return control;
        });

        State = ModuleHostState.Active;
        _logger.LogInformation("Module {ModuleId} activated", _discovery.ModuleId);
        return _cachedView;
    }

    public async Task DeactivateAsync()
    {
        if (_instance is not null)
        {
            try
            {
                await _instance.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down module {ModuleId}", _discovery.ModuleId);
            }

            _instance = null;
            _cachedView = null;
            State = ModuleHostState.Deactivated;
            _logger.LogInformation("Module {ModuleId} deactivated", _discovery.ModuleId);
        }
    }
}
