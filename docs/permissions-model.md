# Permissions Model

## Design Philosophy

The CRATE Workbench permission system is **capability-based** and **lab-scoped**. Code never checks role names — it checks permission keys that represent specific capabilities. Roles are an administrative grouping mechanism that maps users to sets of permissions within a lab.

## Permission Levels

Permission levels form an ordered hierarchy. Higher values include the intent of lower values:

| Level | Value | Meaning |
|-------|-------|---------|
| `Hidden` | 0 | Module is not visible to the user |
| `View` | 1 | Read-only access to module content |
| `LimitedEdit` | 2 | Specific edit actions only |
| `FullEdit` | 3 | All non-admin edit actions |
| `ModuleAdmin` | 4 | Module-level configuration |
| `LabAdmin` | 5 | Lab-level member/role management |

## Lab Scoping

Every permission entity is scoped to a lab:

- **Roles** are defined per-lab (e.g., "viewer" in VSCL is a separate role from "viewer" in Emissions)
- **Role assignments** are per-user, per-lab
- **Permission grants** are per-role, per-lab
- **Module access** is per-lab (a lab must explicitly enable a module)

## Multi-Lab Sessions

Users may belong to multiple labs and select multiple labs at startup. When multiple labs are active:

- **Effective permission level** = maximum level across all active labs
- **Granted permissions** = union of all permissions across all active labs
- **Module visibility** = visible if enabled in any active lab

This additive model means a user with `View` in VSCL and `FullEdit` in Battery sees the module at `FullEdit` level when both labs are active.

## Permission Flow

```
User selects labs at setup
         │
         ▼
IdentityService.SetActiveLabs(labs)
         │
         ▼
Module Discovery (ModuleLoader)
         │
         ▼
For each module:
  PermissionService.GetEffectivePermission(userId, labs, moduleId)
         │
         ├── Hidden → module not shown in nav
         └── View+ → module registered, shown in nav with level
                │
                ▼
         ModuleHost.ActivateAsync
                │
                ▼
         CoreServicesFactory.CreateForModule
                │
                ▼
         PermissionService.CreateContextForModule
                │
                ▼
         Module receives IPermissionContext via ICoreServices.Permissions
```

## Module-Side Permission Checks

Modules check capabilities through `IPermissionContext`, never by inspecting roles:

```csharp
// Check a capability across any active lab
if (coreServices.Permissions.Can("sample.edit"))
{
    // show edit controls
}

// Check a capability in a specific lab
if (coreServices.Permissions.Can("sample.edit", "VSCL"))
{
    // VSCL-specific edit behavior
}

// Check the overall permission level
if (coreServices.Permissions.EffectiveLevel >= PermissionLevel.FullEdit)
{
    // show advanced controls
}
```

## Module Permission Declarations

Modules declare what capabilities they define using assembly-level attributes:

```csharp
[assembly: ModulePermission("sample.view", "View sample content")]
[assembly: ModulePermission("sample.edit", "Edit sample data", PermissionLevel.LimitedEdit)]
```

The `MinimumLevel` parameter means a user must have at least that permission level before the capability can be granted to them.

## Shell-Level Permission Service

The `IPermissionService` interface provides the full CRUD surface for permission management:

- **Resolution**: `GetEffectivePermission`, `CreateContextForModule`
- **Role management**: `GetLabRoles`, `CreateRole`, `GetUserRoles`, `AssignRole`, `RemoveRole`
- **Member management**: `GetLabMembers`, `AddLabMember`, `RemoveLabMember`
- **Permission grants**: `GetRolePermissions`, `GrantPermission`, `RevokePermission`
- **Lab queries**: `GetUserLabs`, `GetAdminLabs`

The current implementation (`PermissionService`) is an in-memory stub seeded with default roles and development data. It will be replaced with a database-backed implementation using the schema in `docs/schema/permissions.sql`.

## Default Roles

The permission service seeds five default roles for each lab:

| Role ID | Display Name | Level |
|---------|-------------|-------|
| `viewer` | Viewer | View |
| `operator` | Operator | LimitedEdit |
| `engineer` | Engineer | FullEdit |
| `module-admin` | Module Admin | ModuleAdmin |
| `lab-admin` | Lab Admin | LabAdmin |

Labs can create additional custom roles via the admin panel.

## Future: Database-Backed Permissions

The reference SQL schema in `docs/schema/permissions.sql` defines the tables for a Postgres-backed implementation. The key query for resolving effective permissions joins `user_lab_roles` → `role_permissions` → `permissions` for a given user and set of lab IDs. See `docs/database-schema.md` for details.
