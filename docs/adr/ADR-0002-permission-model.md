# ADR-0002: Permission Model

## Status

Accepted

## Date

2026-03-16

## Context

The CRATE Workbench serves multiple labs, each with different users, roles, and module access requirements. The permission system must support:

- Lab-scoped roles (a user's role in one lab is independent of their role in another)
- Multi-lab sessions where a user selects multiple labs simultaneously
- Capability-based permission checks in module code (not role-name checks)
- Module-declared permissions (modules define what capabilities exist)
- Shell-level module visibility gating (hide modules users cannot access)
- A future migration from in-memory to database-backed storage

Options considered:

1. **Simple role-based access (RBAC)** — Global roles (admin, editor, viewer). Modules check `if (user.IsAdmin)`. Simple but does not support lab scoping or fine-grained module capabilities.

2. **Capability-based, lab-scoped permissions** — Roles are per-lab. Roles map to permission keys. Modules declare capabilities via attributes. Code checks `Can("sample.edit")` not `IsRole("admin")`. Multi-lab sessions use additive (max) resolution.

3. **Claims-based with external IdP** — Delegate all authorization to an external identity provider. Maximum flexibility but adds external dependency and deployment complexity for a desktop app.

## Decision

**Option 2: Capability-based, lab-scoped permissions.**

The system uses an ordered `PermissionLevel` enum (Hidden through LabAdmin) for coarse access and string-based permission keys for fine-grained capability checks. Modules declare permissions via `[assembly: ModulePermission(...)]` attributes. The `IPermissionContext` interface provides the module-facing API; `IPermissionService` provides the admin-facing API.

For multi-lab sessions, the effective level is the **maximum** across all active labs, and granted permissions are the **union**.

## Consequences

**Positive:**
- Module code is decoupled from authorization policy — it only checks capabilities
- Lab admins can customize roles and permission grants without shell changes
- Multi-lab additive resolution is intuitive (more labs = more access, never less)
- Clean separation between module-declared capabilities and admin-managed grants
- In-memory implementation can be swapped for database-backed without module changes

**Negative:**
- More complex than simple RBAC — requires maintaining role/permission/grant data per lab
- Permission key naming conventions must be enforced by convention (no compile-time validation across module boundaries)
- The in-memory stub seeds development data that may diverge from production role structures

**Risks:**
- Permission key collisions between modules (mitigated by `{module}.{action}` convention)
- Performance concern if a user has many labs and many roles (mitigated by lazy resolution — only resolved at module discovery and context creation time)
