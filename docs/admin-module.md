# Admin Panel

## Overview

The CRATE Workbench includes a centralized admin panel for managing lab memberships, roles, and module access. The admin panel is a shell-owned view (not a module) — it lives in the Workbench project and is always available to users with `LabAdmin` permission on at least one lab.

## Access Control

The admin panel (shield icon in the bottom nav) is only visible when `AdminViewModel.AdminLabs.Count > 0`. The admin VM queries `IPermissionService.GetAdminLabs(userId)` which returns labs where the user holds a role with `PermissionLevel.LabAdmin` or higher.

Users can only manage the labs they are admins of. Selecting a lab from the dropdown scopes all operations to that lab.

## Features

### Member Management

- **View members**: Lists all members of the selected lab with their assigned roles
- **Add member**: Enter a user ID (AD/LDAP username) to add them to the lab
- **Remove member**: Remove a user from the lab (also removes all their role assignments in that lab)

### Role Management

- **View roles**: Lists all roles defined for the selected lab with their permission level
- **Create role**: Define a new role with an ID, display name, and permission level
- **View role permissions**: See which permission keys are granted to each role

### Role Assignment

- **Assign role**: Grant a role to a member within the selected lab
- **Remove role**: Revoke a role from a member

### Module Access

- **View modules**: Lists all discovered modules with their IDs and names
- Module access per lab is managed via `IPermissionService.EnableModuleForLab`
- The Development lab enables all modules by default

## Architecture

```
MainWindowViewModel
  ├── IsAdminVisible (bound to nav button visibility)
  ├── OpenAdminCommand (switches content panel to AdminView)
  └── AdminViewModel
        ├── AdminLabs (labs this user can admin)
        ├── SelectedLab (scopes all operations)
        ├── LabMembers, LabRoles, ModulePermissions
        ├── AddMemberCommand, RemoveMemberCommand
        ├── CreateRoleCommand, AssignRoleCommand
        └── Refresh() / LoadLabDetails(labId)
```

The `AdminView` renders in the same right-panel content area as modules. When the admin button is clicked, the selected module is deselected and the admin view replaces the module content.

## Why Shell-Owned (Not a Module)

The admin panel is intentionally part of the shell rather than loaded as a module:

1. **Cross-cutting concern** — It manages permissions for all modules, not just one
2. **Bootstrapping** — It needs to be available before any modules are loaded
3. **Security boundary** — Shell-owned code has direct access to `IPermissionService`; modules only get the read-only `IPermissionContext`
4. **No isolation needed** — Admin operations are shell operations, not user-space module operations

See ADR-0003 for the full decision rationale.

## Display Records

The admin VM uses display-specific records to decouple the view from the service layer:

- `LabMemberDisplay(UserId, Roles)` — flattened role list as comma-separated string
- `LabRoleDisplay(RoleId, DisplayName, Level, Permissions)` — role with its granted permission keys
- `ModulePermissionDisplay(ModuleId, ModuleName)` — module identity for the access grid
