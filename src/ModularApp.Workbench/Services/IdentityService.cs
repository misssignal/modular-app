using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Identity derived from AD/LDAP username and machine hostname.
/// Roles are stubbed for v1; will be fetched from a central API in the future.
/// </summary>
public class IdentityService : IIdentityProvider
{
    public string Username { get; } = Environment.UserName;
    public string Hostname { get; } = Environment.MachineName;
    public IReadOnlyList<string> Roles { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> ActiveLabs { get; private set; } = Array.Empty<string>();

    /// <summary>Set the active labs from the setup profile (called at startup).</summary>
    public void SetActiveLabs(IReadOnlyList<string> labs)
    {
        ActiveLabs = labs;
    }

    public bool HasClaim(string claimType, string claimValue)
    {
        return claimType switch
        {
            "role" => Roles.Contains(claimValue, StringComparer.OrdinalIgnoreCase),
            "lab" => ActiveLabs.Contains(claimValue, StringComparer.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
