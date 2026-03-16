using System.Reflection;
using System.Runtime.Loader;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Custom AssemblyLoadContext that isolates each module's dependencies.
/// Shared assemblies (SDK, UI, Avalonia) fall through to the default context
/// to ensure type identity across the module boundary.
/// </summary>
public class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    private static readonly HashSet<string> SharedAssemblyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ModularApp.Sdk",
        "ModularApp.Ui",
        "Avalonia",
        "CommunityToolkit.Mvvm",
        "Microsoft.Extensions.Logging",
        "Microsoft.Extensions.DependencyInjection",
        "Semver",
    };

    public ModuleLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        if (name is null) return null;

        // Shared assemblies must come from the default context
        // to ensure type identity across the module boundary
        foreach (var prefix in SharedAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null; // fall through to default context
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
