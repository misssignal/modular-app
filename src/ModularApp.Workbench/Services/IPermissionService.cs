using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Shell-level permission service that resolves user permissions
/// based on lab memberships, roles, and module capability declarations.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Get the effective permission level for a user on a specific module,
    /// taking the maximum across all specified labs.
    /// </summary>
    PermissionLevel GetEffectivePermission(string userId, IReadOnlyList<string> labs, string moduleId);

    /// <summary>
    /// Create a module-scoped permission context for use in ICoreServices.
    /// </summary>
    IPermissionContext CreateContextForModule(string userId, IReadOnlyList<string> labs, string moduleId);

    /// <summary>Get all roles for a user within a specific lab.</summary>
    IReadOnlyList<LabRole> GetUserRoles(string userId, string labId);

    /// <summary>Get all members of a lab.</summary>
    IReadOnlyList<LabMember> GetLabMembers(string labId);

    /// <summary>Get all roles defined for a lab.</summary>
    IReadOnlyList<LabRole> GetLabRoles(string labId);

    /// <summary>Get the permission grants for a role in a lab.</summary>
    IReadOnlyList<PermissionGrant> GetRolePermissions(string roleId, string labId);

    /// <summary>Assign a role to a user in a lab.</summary>
    void AssignRole(string userId, string labId, string roleId);

    /// <summary>Remove a role from a user in a lab.</summary>
    void RemoveRole(string userId, string labId, string roleId);

    /// <summary>Grant a permission to a role in a lab.</summary>
    void GrantPermission(string roleId, string labId, string permissionKey);

    /// <summary>Revoke a permission from a role in a lab.</summary>
    void RevokePermission(string roleId, string labId, string permissionKey);

    /// <summary>Add a member to a lab.</summary>
    void AddLabMember(string userId, string labId);

    /// <summary>Remove a member from a lab.</summary>
    void RemoveLabMember(string userId, string labId);

    /// <summary>Create a new role in a lab.</summary>
    void CreateRole(string roleId, string labId, string displayName, PermissionLevel level);

    /// <summary>Get all labs the user is a member of.</summary>
    IReadOnlyList<string> GetUserLabs(string userId);

    /// <summary>Get all labs the user can administer.</summary>
    IReadOnlyList<string> GetAdminLabs(string userId);
}

public record LabRole(string RoleId, string LabId, string DisplayName, PermissionLevel Level);
public record LabMember(string UserId, string LabId, IReadOnlyList<string> RoleIds);
public record PermissionGrant(string RoleId, string LabId, string PermissionKey);
