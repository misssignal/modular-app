namespace ModularApp.Sdk;

/// <summary>
/// Provides identity information derived from AD/LDAP username
/// and machine hostname, with centrally managed claims/roles.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>Current user's AD/LDAP username.</summary>
    string Username { get; }

    /// <summary>Machine hostname.</summary>
    string Hostname { get; }

    /// <summary>Centrally managed roles assigned to this user.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Labs active in the current session (selected at setup).</summary>
    IReadOnlyList<string> ActiveLabs { get; }

    /// <summary>Check whether the user has a specific claim.</summary>
    bool HasClaim(string claimType, string claimValue);
}
