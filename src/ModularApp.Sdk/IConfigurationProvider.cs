namespace ModularApp.Sdk;

/// <summary>
/// Read-only configuration provider for modules.
/// Configuration is authoritative from the repository (config-as-code)
/// and is not editable at runtime.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>Get a typed configuration section by key.</summary>
    T? GetSection<T>(string sectionKey) where T : class;

    /// <summary>Get a single string value by key.</summary>
    string? GetValue(string key);
}
