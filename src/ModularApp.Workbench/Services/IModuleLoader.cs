namespace ModularApp.Workbench.Services;

public interface IModuleLoader
{
    Task<IReadOnlyList<ModuleDiscoveryResult>> DiscoverModulesAsync();
}
