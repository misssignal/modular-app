using Microsoft.Extensions.Logging;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Tracks all discovered modules and their lifecycle hosts.
/// </summary>
public class ModuleRegistry : IModuleRegistry
{
    private readonly Dictionary<string, ModuleHost> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICoreServicesFactory _coreServicesFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ModuleRegistry(ICoreServicesFactory coreServicesFactory, ILoggerFactory loggerFactory)
    {
        _coreServicesFactory = coreServicesFactory;
        _loggerFactory = loggerFactory;
    }

    public void Register(ModuleDiscoveryResult discovery)
    {
        if (!_hosts.ContainsKey(discovery.ModuleId))
        {
            var logger = _loggerFactory.CreateLogger($"ModuleHost.{discovery.ModuleId}");
            _hosts[discovery.ModuleId] = new ModuleHost(discovery, _coreServicesFactory, logger);
        }
    }

    public ModuleHost? GetHost(string moduleId)
    {
        return _hosts.GetValueOrDefault(moduleId);
    }

    public IReadOnlyList<ModuleHost> GetAllHosts()
    {
        return _hosts.Values.ToList().AsReadOnly();
    }
}
