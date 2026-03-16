namespace ModularApp.Workbench.Services;

public interface IModuleRegistry
{
    void Register(ModuleDiscoveryResult discovery);
    ModuleHost? GetHost(string moduleId);
    IReadOnlyList<ModuleHost> GetAllHosts();
}
