namespace ModularApp.Sdk;

/// <summary>
/// Declares a permission capability that this module defines.
/// Applied at the assembly level. The Admin module reads these declarations
/// to populate the permission management UI.
/// <para>
/// Modules declare what permissions exist; the admin system manages who has them.
/// Modules never reference role names — only permission keys.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ModulePermissionAttribute : Attribute
{
    /// <summary>
    /// Unique permission key, scoped to this module.
    /// Convention: "{module-short-name}.{action}" e.g. "test-request.edit"
    /// </summary>
    public string PermissionKey { get; }

    /// <summary>Human-readable description shown in admin UI.</summary>
    public string Description { get; }

    /// <summary>
    /// The minimum permission level required to hold this capability.
    /// Users below this level will never have this permission granted.
    /// </summary>
    public PermissionLevel MinimumLevel { get; }

    public ModulePermissionAttribute(
        string permissionKey,
        string description,
        PermissionLevel minimumLevel = PermissionLevel.View)
    {
        PermissionKey = permissionKey;
        Description = description;
        MinimumLevel = minimumLevel;
    }
}
