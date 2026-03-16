namespace ModularApp.Sdk;

/// <summary>
/// Central contract that every module must implement.
/// The shell discovers and manages modules through this interface.
/// </summary>
public interface IModule
{
    /// <summary>Unique identifier (e.g. "modularapp.module.sample").</summary>
    string Id { get; }

    /// <summary>Human-readable display name shown in the nav.</summary>
    string Name { get; }

    /// <summary>SemVer version of this module.</summary>
    string Version { get; }

    /// <summary>
    /// SemVer range string declaring compatible engine versions
    /// (e.g. ">=1.0.0 &lt;2.0.0").
    /// </summary>
    string CompatibleEngineVersions { get; }

    /// <summary>Icon key for display in the navigation panel.</summary>
    string IconKey { get; }

    /// <summary>
    /// Called once when the module is first loaded. Receives core services
    /// provided by the shell (logging, config, identity, navigation).
    /// </summary>
    Task InitializeAsync(ICoreServices coreServices);

    /// <summary>
    /// Called when the module is activated (user clicks it in the nav).
    /// Returns the root UI control for the right panel.
    /// Return type is <c>object</c> to keep the SDK Avalonia-free;
    /// the shell casts it to <c>Avalonia.Controls.Control</c>.
    /// </summary>
    object CreateView();

    /// <summary>
    /// Called when the module is being unloaded (shell shutdown or module reload).
    /// </summary>
    Task ShutdownAsync();
}
