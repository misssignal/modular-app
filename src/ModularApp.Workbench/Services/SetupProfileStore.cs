using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Reads and writes the setup profile to ~/.crate/setup.json.
/// Returns null on Load() if the file does not exist (first run).
/// </summary>
public sealed class SetupProfileStore
{
    private static readonly string CrateDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".crate");

    private static readonly string ProfilePath =
        Path.Combine(CrateDir, "setup.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<SetupProfileStore> _logger;

    public SetupProfileStore(ILogger<SetupProfileStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load the saved setup profile. Returns null if no profile exists (first run).
    /// </summary>
    public SetupProfile? Load()
    {
        if (!File.Exists(ProfilePath))
        {
            _logger.LogInformation("No setup profile found at {Path} — first-time setup required", ProfilePath);
            return null;
        }

        try
        {
            var json = File.ReadAllText(ProfilePath);
            var profile = JsonSerializer.Deserialize<SetupProfile>(json, JsonOptions);

            if (profile is null || !profile.IsValid)
            {
                _logger.LogWarning("Setup profile at {Path} is invalid — first-time setup required", ProfilePath);
                return null;
            }

            _logger.LogInformation("Loaded setup profile for bench {BenchName} in {LabArea}", profile.BenchName, profile.LabArea);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read setup profile from {Path}", ProfilePath);
            return null;
        }
    }

    /// <summary>
    /// Save the setup profile. Auto-populates Username and Hostname from the environment.
    /// </summary>
    public void Save(SetupProfile profile)
    {
        profile.Username = Environment.UserName;
        profile.Hostname = Environment.MachineName;

        Directory.CreateDirectory(CrateDir);

        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(ProfilePath, json);

        _logger.LogInformation("Saved setup profile to {Path}", ProfilePath);
    }

    /// <summary>
    /// Delete the setup profile file, forcing first-time setup on next launch.
    /// </summary>
    public bool Delete()
    {
        if (!File.Exists(ProfilePath))
            return false;

        File.Delete(ProfilePath);
        _logger.LogInformation("Deleted setup profile at {Path}", ProfilePath);
        return true;
    }

    /// <summary>Full path to the profile file (for diagnostics).</summary>
    public static string FilePath => ProfilePath;
}
