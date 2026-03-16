using Microsoft.Extensions.Logging;
using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

/// <summary>
/// In-memory stub implementation of IPermissionService.
/// Seeds default roles and permissions for development.
/// Will be replaced with database-backed implementation.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly ILogger<PermissionService> _logger;

    // In-memory stores (replace with DB)
    private readonly Dictionary<string, HashSet<string>> _labMembers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string RoleId, string LabId), LabRole> _roles = new();
    private readonly Dictionary<(string UserId, string LabId), HashSet<string>> _userRoles = new();
    private readonly Dictionary<(string RoleId, string LabId), HashSet<string>> _rolePermissions = new();

    // Module → lab → enabled
    private readonly Dictionary<(string ModuleId, string LabId), bool> _labModules = new();

    public PermissionService(ILogger<PermissionService> logger)
    {
        _logger = logger;
        SeedDefaults();
    }

    private void SeedDefaults()
    {
        var labs = new[] { "VSCL", "Emissions", "Battery", "BSL", "Development" };

        foreach (var lab in labs)
        {
            _labMembers[lab] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Default roles per lab
            CreateRoleInternal("viewer", lab, "Viewer", PermissionLevel.View);
            CreateRoleInternal("operator", lab, "Operator", PermissionLevel.LimitedEdit);
            CreateRoleInternal("engineer", lab, "Engineer", PermissionLevel.FullEdit);
            CreateRoleInternal("module-admin", lab, "Module Admin", PermissionLevel.ModuleAdmin);
            CreateRoleInternal("lab-admin", lab, "Lab Admin", PermissionLevel.LabAdmin);
        }

        // Seed the current user as admin of Development lab
        var currentUser = Environment.UserName;
        AddLabMember(currentUser, "Development");
        AssignRole(currentUser, "Development", "lab-admin");

        // Enable sample module in Development
        _labModules[("modularapp.module.sample", "Development")] = true;

        // Grant all sample module permissions to lab-admin in Development
        GrantPermission("lab-admin", "Development", "sample.view");
        GrantPermission("lab-admin", "Development", "sample.edit");

        _logger.LogInformation("Permission service seeded with default roles for {Count} labs", labs.Length);
    }

    public PermissionLevel GetEffectivePermission(string userId, IReadOnlyList<string> labs, string moduleId)
    {
        var maxLevel = PermissionLevel.Hidden;

        foreach (var lab in labs)
        {
            // Check if module is enabled for this lab
            if (!IsModuleEnabledForLab(moduleId, lab))
                continue;

            // Check if user is a member of this lab
            if (!_labMembers.TryGetValue(lab, out var members) || !members.Contains(userId))
                continue;

            // Get user's roles in this lab
            if (!_userRoles.TryGetValue((userId, lab), out var roleIds))
                continue;

            foreach (var roleId in roleIds)
            {
                if (_roles.TryGetValue((roleId, lab), out var role) && role.Level > maxLevel)
                    maxLevel = role.Level;
            }
        }

        return maxLevel;
    }

    public IPermissionContext CreateContextForModule(string userId, IReadOnlyList<string> labs, string moduleId)
    {
        var effectiveLevel = GetEffectivePermission(userId, labs, moduleId);
        var grantedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var lab in labs)
        {
            if (!_userRoles.TryGetValue((userId, lab), out var roleIds))
                continue;

            foreach (var roleId in roleIds)
            {
                if (_rolePermissions.TryGetValue((roleId, lab), out var perms))
                    grantedPermissions.UnionWith(perms);
            }
        }

        return new PermissionContext(labs, effectiveLevel, grantedPermissions, userId, moduleId, this);
    }

    public IReadOnlyList<LabRole> GetUserRoles(string userId, string labId)
    {
        if (!_userRoles.TryGetValue((userId, labId), out var roleIds))
            return Array.Empty<LabRole>();

        return roleIds
            .Select(rid => _roles.GetValueOrDefault((rid, labId)))
            .Where(r => r is not null)
            .ToList()!;
    }

    public IReadOnlyList<LabMember> GetLabMembers(string labId)
    {
        if (!_labMembers.TryGetValue(labId, out var members))
            return Array.Empty<LabMember>();

        return members.Select(uid =>
        {
            var roles = _userRoles.TryGetValue((uid, labId), out var rids)
                ? rids.ToList().AsReadOnly() as IReadOnlyList<string>
                : Array.Empty<string>();
            return new LabMember(uid, labId, roles);
        }).ToList();
    }

    public IReadOnlyList<LabRole> GetLabRoles(string labId)
    {
        return _roles.Values.Where(r => r.LabId.Equals(labId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public IReadOnlyList<PermissionGrant> GetRolePermissions(string roleId, string labId)
    {
        if (!_rolePermissions.TryGetValue((roleId, labId), out var perms))
            return Array.Empty<PermissionGrant>();

        return perms.Select(pk => new PermissionGrant(roleId, labId, pk)).ToList();
    }

    public void AssignRole(string userId, string labId, string roleId)
    {
        var key = (userId, labId);
        if (!_userRoles.ContainsKey(key))
            _userRoles[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _userRoles[key].Add(roleId);
        _logger.LogInformation("Assigned role {RoleId} to {UserId} in {LabId}", roleId, userId, labId);
    }

    public void RemoveRole(string userId, string labId, string roleId)
    {
        if (_userRoles.TryGetValue((userId, labId), out var roles))
        {
            roles.Remove(roleId);
            _logger.LogInformation("Removed role {RoleId} from {UserId} in {LabId}", roleId, userId, labId);
        }
    }

    public void GrantPermission(string roleId, string labId, string permissionKey)
    {
        var key = (roleId, labId);
        if (!_rolePermissions.ContainsKey(key))
            _rolePermissions[key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _rolePermissions[key].Add(permissionKey);
    }

    public void RevokePermission(string roleId, string labId, string permissionKey)
    {
        if (_rolePermissions.TryGetValue((roleId, labId), out var perms))
            perms.Remove(permissionKey);
    }

    public void AddLabMember(string userId, string labId)
    {
        if (!_labMembers.ContainsKey(labId))
            _labMembers[labId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _labMembers[labId].Add(userId);
        _logger.LogInformation("Added {UserId} to lab {LabId}", userId, labId);
    }

    public void RemoveLabMember(string userId, string labId)
    {
        if (_labMembers.TryGetValue(labId, out var members))
        {
            members.Remove(userId);
            // Also remove all roles
            _userRoles.Remove((userId, labId));
            _logger.LogInformation("Removed {UserId} from lab {LabId}", userId, labId);
        }
    }

    public void CreateRole(string roleId, string labId, string displayName, PermissionLevel level)
    {
        CreateRoleInternal(roleId, labId, displayName, level);
    }

    public IReadOnlyList<string> GetUserLabs(string userId)
    {
        return _labMembers
            .Where(kv => kv.Value.Contains(userId))
            .Select(kv => kv.Key)
            .ToList();
    }

    public IReadOnlyList<string> GetAdminLabs(string userId)
    {
        return _userRoles
            .Where(kv => kv.Key.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
            .Where(kv => kv.Value.Any(rid =>
                _roles.TryGetValue((rid, kv.Key.LabId), out var role) &&
                role.Level >= PermissionLevel.LabAdmin))
            .Select(kv => kv.Key.LabId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Enable a module for a lab.</summary>
    public void EnableModuleForLab(string moduleId, string labId)
    {
        _labModules[(moduleId, labId)] = true;
    }

    private bool IsModuleEnabledForLab(string moduleId, string labId)
    {
        // In Development, all modules are enabled by default
        if (labId.Equals("Development", StringComparison.OrdinalIgnoreCase))
            return true;

        return _labModules.TryGetValue((moduleId, labId), out var enabled) && enabled;
    }

    private void CreateRoleInternal(string roleId, string labId, string displayName, PermissionLevel level)
    {
        _roles[(roleId, labId)] = new LabRole(roleId, labId, displayName, level);
    }
}

/// <summary>
/// Module-scoped permission context passed to modules via ICoreServices.
/// </summary>
internal sealed class PermissionContext : IPermissionContext
{
    private readonly string _userId;
    private readonly string _moduleId;
    private readonly PermissionService _service;

    public IReadOnlyList<string> ActiveLabs { get; }
    public PermissionLevel EffectiveLevel { get; }
    public IReadOnlySet<string> GrantedPermissions { get; }

    public PermissionContext(
        IReadOnlyList<string> activeLabs,
        PermissionLevel effectiveLevel,
        HashSet<string> grantedPermissions,
        string userId,
        string moduleId,
        PermissionService service)
    {
        ActiveLabs = activeLabs;
        EffectiveLevel = effectiveLevel;
        GrantedPermissions = grantedPermissions;
        _userId = userId;
        _moduleId = moduleId;
        _service = service;
    }

    public bool Can(string permissionKey) => GrantedPermissions.Contains(permissionKey);

    public bool Can(string permissionKey, string labId)
    {
        // Re-evaluate for a specific lab
        var context = _service.CreateContextForModule(_userId, new[] { labId }, _moduleId);
        return context.GrantedPermissions.Contains(permissionKey);
    }

    public PermissionLevel EffectiveLevelForLab(string labId)
    {
        return _service.GetEffectivePermission(_userId, new[] { labId }, _moduleId);
    }
}
