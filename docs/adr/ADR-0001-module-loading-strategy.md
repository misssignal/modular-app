# ADR-0001: Module Loading Strategy

## Status

Accepted

## Date

2026-03-16

## Context

The CRATE Workbench needs to load feature modules from external assemblies at runtime. Each lab team builds and distributes their own modules independently. The shell must support:

- Loading modules without recompiling or redeploying the shell
- Isolating module dependencies so that two modules can use different versions of the same library
- Sharing core types (SDK interfaces, Avalonia controls) so the shell can interact with module objects
- Version gating to prevent incompatible modules from loading
- Future support for hot-reload (unloading and reloading a module without restarting)

Options considered:

1. **MEF (Managed Extensibility Framework)** — Built-in .NET composition model. Well-understood but lacks dependency isolation and does not support collectible contexts.

2. **Custom AssemblyLoadContext with shared passthrough** — Each module loads into its own `AssemblyLoadContext` with `isCollectible: true`. Shared assemblies (SDK, UI, Avalonia, CommunityToolkit) fall through to the default context. Module-specific dependencies resolve within the isolated context.

3. **Process-level isolation** — Run each module in a separate process and use IPC. Maximum isolation but high complexity and latency for UI rendering.

## Decision

**Option 2: Custom `AssemblyLoadContext` with shared passthrough.**

The `ModuleLoadContext` class extends `AssemblyLoadContext` and overrides `Load()` to return `null` for assemblies matching shared prefixes, causing the runtime to resolve them from the default context. All other assemblies are resolved by `AssemblyDependencyResolver` within the module's own directory.

The loader performs version gating by temporarily instantiating the `IModule` type at discovery time and checking `CompatibleEngineVersions` against the engine version using SemVer range parsing.

## Consequences

**Positive:**
- Full dependency isolation for module-specific libraries
- Type identity preserved for shared interfaces (a module's `IModule` is the same type the shell expects)
- `isCollectible: true` enables future hot-reload via `Unload()`
- Fast metadata discovery via `ModuleMetadataAttribute` — no need to instantiate the full module for identity

**Negative:**
- Shared assembly list must be maintained manually — adding a new shared dependency requires updating `ModuleLoadContext.SharedAssemblyPrefixes`
- Version gating requires temporary instantiation, which runs module constructors (mitigated by keeping constructors lightweight)
- Debugging across load context boundaries can be harder in IDE tooling

**Risks:**
- Native library conflicts if two modules need different versions of the same native DLL (unmanaged resolution is less flexible)
- Assembly version mismatches in shared assemblies between shell and module builds could cause runtime failures
