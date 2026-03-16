namespace ModularApp.Sdk;

/// <summary>
/// Provides permission evaluation for the current user session.
/// Modules use this to check capabilities — never role names.
/// Permissions are always lab-scoped and resolved at the capability level.
/// </summary>
public interface IPermissionContext
{
    /// <summary>Labs active in the current session.</summary>
    IReadOnlyList<string> ActiveLabs { get; }

    /// <summary>
    /// Check whether the user has a specific capability across any active lab.
    /// Returns true if any active lab grants this permission.
    /// </summary>
    bool Can(string permissionKey);

    /// <summary>
    /// Check whether the user has a specific capability within a specific lab.
    /// </summary>
    bool Can(string permissionKey, string labId);

    /// <summary>
    /// The effective (maximum) permission level for the current module
    /// across all active labs.
    /// </summary>
    PermissionLevel EffectiveLevel { get; }

    /// <summary>
    /// The effective permission level for the current module within a specific lab.
    /// </summary>
    PermissionLevel EffectiveLevelForLab(string labId);

    /// <summary>
    /// Returns all granted permission keys for the current module across active labs.
    /// </summary>
    IReadOnlySet<string> GrantedPermissions { get; }
}
