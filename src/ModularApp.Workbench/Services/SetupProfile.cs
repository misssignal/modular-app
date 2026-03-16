using System.Text.Json.Serialization;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Persisted first-time setup data: auth token, bench identity,
/// lab/work-area selection, and module source URI.
/// </summary>
public sealed class SetupProfile
{
    public required string Token { get; set; }
    public required string BenchName { get; set; }
    public required string Username { get; set; }
    public required string Hostname { get; set; }

    /// <summary>Selected labs for the current session (multi-select).</summary>
    public required List<string> SelectedLabs { get; set; }

    /// <summary>Module source URIs keyed by lab name.</summary>
    public required Dictionary<string, string> ModuleSources { get; set; }

    // Back-compat: single lab accessor for legacy code paths
    [JsonIgnore]
    public string LabArea => SelectedLabs.FirstOrDefault() ?? string.Empty;

    [JsonIgnore]
    public string ModuleSourceUri => ModuleSources.Values.FirstOrDefault() ?? string.Empty;

    [JsonIgnore]
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Token) &&
        !string.IsNullOrWhiteSpace(BenchName) &&
        SelectedLabs.Count > 0;
}

/// <summary>
/// A lab/work-area option with its default module source URI.
/// Loaded from workbench.json configuration.
/// </summary>
public sealed class LabAreaOption
{
    public required string Name { get; set; }
    public required string ModuleSourceUri { get; set; }
}
