# ModularApp.Sdk

Shared contracts and interfaces referenced by all CRATE Workbench modules. This package defines the boundary between the shell and module code.

## Key Types

| Type | Purpose |
|------|---------|
| `IModule` | Core contract every module implements |
| `ICoreServices` | Shell-provided services injected into modules |
| `IPermissionContext` | Module-scoped permission evaluation |
| `IIdentityProvider` | Current user identity and claims |
| `IConfigurationProvider` | Read-only module configuration |
| `PermissionLevel` | Ordered access level enum (Hidden → LabAdmin) |
| `ModuleMetadataAttribute` | Assembly-level attribute for fast module discovery |
| `ModulePermissionAttribute` | Assembly-level attribute declaring module capabilities |
| `VersionCompatibility` | SemVer range checking utility |
| `ICrateClient` | CRATE backend client interface |

## Design Constraints

- **No Avalonia dependency** — `IModule.CreateView()` returns `object`, not `Control`
- **No mutation APIs** — Modules get read-only `IPermissionContext`, not the full `IPermissionService`
- **Logging abstractions only** — Uses `Microsoft.Extensions.Logging.Abstractions`

## For Module Authors

Reference this package (via NuGet or project reference) and implement `IModule`. See `modules/ModularApp.Module.Sample/` for a complete example and `docs/module-system.md` for the full developer guide.
