using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Scans the modules directory, loads assemblies into isolated AssemblyLoadContexts,
/// reads ModuleMetadataAttribute, and performs version gating.
/// </summary>
public class ModuleLoader : IModuleLoader
{
    private readonly string _modulesDirectory;
    private readonly string _engineVersion;
    private readonly ILogger<ModuleLoader> _logger;

    public ModuleLoader(IConfiguration config, ILogger<ModuleLoader> logger)
    {
        _logger = logger;
        _modulesDirectory = config["Modules:Directory"]
            ?? Path.Combine(AppContext.BaseDirectory, "modules");

        _engineVersion = typeof(ModuleLoader).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0";

        // Strip any build metadata (e.g. "+sha.abc123") that isn't valid for SemVer range checks
        var plusIndex = _engineVersion.IndexOf('+');
        if (plusIndex >= 0)
            _engineVersion = _engineVersion[..plusIndex];
    }

    public Task<IReadOnlyList<ModuleDiscoveryResult>> DiscoverModulesAsync()
    {
        var results = new List<ModuleDiscoveryResult>();

        if (!Directory.Exists(_modulesDirectory))
        {
            _logger.LogWarning("Modules directory does not exist: {Path}", _modulesDirectory);
            return Task.FromResult<IReadOnlyList<ModuleDiscoveryResult>>(results);
        }

        foreach (var dir in Directory.GetDirectories(_modulesDirectory))
        {
            try
            {
                var result = TryLoadModule(dir);
                if (result is not null)
                    results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load module from {Directory}", dir);
            }
        }

        return Task.FromResult<IReadOnlyList<ModuleDiscoveryResult>>(results);
    }

    private ModuleDiscoveryResult? TryLoadModule(string directory)
    {
        var dllPath = FindModuleDll(directory);
        if (dllPath is null)
        {
            _logger.LogDebug("No module DLL found in {Directory}", directory);
            return null;
        }

        var context = new ModuleLoadContext(dllPath);
        Assembly assembly;

        try
        {
            assembly = context.LoadFromAssemblyPath(dllPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assembly {Path}", dllPath);
            context.Unload();
            return null;
        }

        var attr = assembly.GetCustomAttribute<ModuleMetadataAttribute>();
        if (attr is null)
        {
            _logger.LogWarning("Assembly {Path} has no ModuleMetadataAttribute", dllPath);
            context.Unload();
            return null;
        }

        // Instantiate the module temporarily to read its properties
        var moduleType = assembly.GetType(attr.ModuleType);
        if (moduleType is null)
        {
            _logger.LogWarning("Module type {Type} not found in {Path}", attr.ModuleType, dllPath);
            context.Unload();
            return null;
        }

        IModule moduleInstance;
        try
        {
            moduleInstance = (IModule)Activator.CreateInstance(moduleType)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to instantiate module type {Type}", attr.ModuleType);
            context.Unload();
            return null;
        }

        // Version gate
        try
        {
            if (!VersionCompatibility.IsCompatible(_engineVersion, moduleInstance.CompatibleEngineVersions))
            {
                _logger.LogWarning(
                    "Module {ModuleId} requires engine {Range} but current engine is {Version}. Skipping.",
                    attr.ModuleId, moduleInstance.CompatibleEngineVersions, _engineVersion);
                context.Unload();
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse version range for module {ModuleId}", attr.ModuleId);
            context.Unload();
            return null;
        }

        return new ModuleDiscoveryResult(
            ModuleId: attr.ModuleId,
            ModuleName: moduleInstance.Name,
            ModuleVersion: moduleInstance.Version,
            ModuleTypeName: attr.ModuleType,
            IconKey: moduleInstance.IconKey,
            DllPath: dllPath,
            ModuleDirectory: directory,
            LoadContext: context);
    }

    private static string? FindModuleDll(string directory)
    {
        return Directory.GetFiles(directory, "ModularApp.Module.*.dll").FirstOrDefault();
    }
}
