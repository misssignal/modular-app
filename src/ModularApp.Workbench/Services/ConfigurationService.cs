using System.Text.Json;
using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Read-only configuration provider. Reads from environment config files
/// shipped with each module (config-as-code, not user-editable at runtime).
/// </summary>
public class ConfigurationService : Sdk.IConfigurationProvider
{
    private readonly JsonDocument? _moduleConfig;

    public ConfigurationService(string moduleId, string moduleDirectory)
    {
        var configPath = Path.Combine(moduleDirectory, "config", $"{moduleId}.json");
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            _moduleConfig = JsonDocument.Parse(json);
        }
    }

    public T? GetSection<T>(string sectionKey) where T : class
    {
        if (_moduleConfig is null) return null;

        if (_moduleConfig.RootElement.TryGetProperty(sectionKey, out var section))
        {
            return JsonSerializer.Deserialize<T>(section.GetRawText());
        }

        return null;
    }

    public string? GetValue(string key)
    {
        if (_moduleConfig is null) return null;

        if (_moduleConfig.RootElement.TryGetProperty(key, out var value))
        {
            return value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.GetRawText();
        }

        return null;
    }
}
