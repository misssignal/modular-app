# ADR-0003: Admin Panel Location

## Status

Accepted

## Date

2026-03-16

## Context

The CRATE Workbench needs an administration interface for managing lab memberships, roles, and module permissions. The question is whether this admin functionality should be:

1. **A loadable module** — Built as a standard `IModule` implementation, discovered and loaded like any other module
2. **A shell-owned panel** — Built directly into the Workbench project, rendered in the same content area as modules but not loaded through the module system

Key considerations:

- The admin panel manages permissions for all modules (cross-cutting)
- It needs direct access to `IPermissionService` mutation methods (not just the read-only `IPermissionContext` modules receive)
- It must be available before any modules are loaded
- It should only be visible to lab admins

## Decision

**Option 2: Shell-owned panel.**

The admin panel is implemented as `AdminView` + `AdminViewModel` in the Workbench project. It renders in the same right-panel content area as modules (so the UX is consistent) but is wired directly by `App.ActivateAndShowShellAsync`, not by the module loader.

The admin button (shield icon) appears in the bottom nav alongside the settings gear, visible only when `IsAdminVisible` is true (the user has LabAdmin on at least one lab).

## Consequences

**Positive:**
- Direct access to `IPermissionService` — no need to expose admin APIs through the module SDK
- Always available — no dependency on module discovery or loading
- Clear security boundary — admin operations run in shell context, not in an isolated module context
- Simpler testing — no need to deploy the admin as a separate assembly

**Negative:**
- Cannot be independently versioned or hot-reloaded like a module
- Admin UI changes require a shell rebuild and redeploy
- Slightly increases the shell's codebase size

**Alternatives rejected:**
- Making admin a module would require exposing `IPermissionService` (with mutation methods) through `ICoreServices`, which breaks the read-only permission contract modules have today
- A hybrid approach (admin as module with special elevated services) would add complexity without clear benefit given that admin is a core shell concern
