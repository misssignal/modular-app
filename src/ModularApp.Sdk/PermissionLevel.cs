namespace ModularApp.Sdk;

/// <summary>
/// Ordered permission levels for module access.
/// Higher values grant broader access. Levels are additive across labs —
/// a user's effective level is the maximum across their selected labs.
/// </summary>
public enum PermissionLevel
{
    /// <summary>Module is not visible to the user.</summary>
    Hidden = 0,

    /// <summary>Read-only access to module content.</summary>
    View = 1,

    /// <summary>Limited edit capabilities (specific actions only).</summary>
    LimitedEdit = 2,

    /// <summary>Full edit capabilities (all non-admin actions).</summary>
    FullEdit = 3,

    /// <summary>Module-level administration (configure module behavior).</summary>
    ModuleAdmin = 4,

    /// <summary>Lab-level administration (manage members, roles, permissions).</summary>
    LabAdmin = 5,
}
