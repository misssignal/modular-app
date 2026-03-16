using System.Runtime.Loader;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Result of discovering a module in the modules directory.
/// </summary>
public record ModuleDiscoveryResult(
    string ModuleId,
    string ModuleName,
    string ModuleVersion,
    string ModuleTypeName,
    string IconKey,
    string DllPath,
    string ModuleDirectory,
    AssemblyLoadContext LoadContext);
