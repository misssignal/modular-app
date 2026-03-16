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

    public bool HasClaim(string claimType, string claimValue)
    {
        return claimType == "role" && Roles.Contains(claimValue, StringComparer.OrdinalIgnoreCase);
    }
}
