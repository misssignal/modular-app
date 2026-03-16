# Module System

## Overview

The CRATE Workbench module system allows labs to build self-contained feature modules that are discovered, loaded, and managed by the shell at runtime. Modules are compiled separately, distributed as DLLs, and run in isolated `AssemblyLoadContext` instances to prevent dependency conflicts.

## SDK Contract

Every module implements `IModule` from the `ModularApp.Sdk` package:

| Member | Type | Purpose |
|--------|------|---------|
| `Id` | `string` | Unique identifier, e.g. `modularapp.module.sample` |
| `Name` | `string` | Display name shown in the navigation panel |
| `Version` | `string` | SemVer version of the module |
| `CompatibleEngineVersions` | `string` | SemVer range the module supports, e.g. `>=1.0.0 <2.0.0` |
| `IconKey` | `string` | Icon key for nav display |
| `InitializeAsync(ICoreServices)` | `Task` | Called once at load time with shell-provided services |
| `CreateView()` | `object` | Returns the root UI control (cast to Avalonia `Control` by the shell) |
| `ShutdownAsync()` | `Task` | Called on unload for cleanup |

The `CreateView()` return type is `object` intentionally — it keeps the SDK free of any Avalonia dependency so that module authors only need to reference `ModularApp.Sdk` and `ModularApp.Ui`.

## Assembly-Level Metadata

Modules declare identity via `ModuleMetadataAttribute` in `Properties/AssemblyInfo.cs`:

```csharp
[assembly: ModuleMetadata(
    "modularapp.module.sample",
    "ModularApp.Module.Sample.SampleModule")]
```

This attribute enables the loader to read module identity without instantiating any types — it only needs to reflect over the assembly metadata.

## Permission Declarations

Modules declare their capabilities via `ModulePermissionAttribute`:

```csharp
[assembly: ModulePermission("sample.view", "View sample module content")]
[assembly: ModulePermission("sample.edit", "Edit sample module data",
    PermissionLevel.LimitedEdit)]
```

These declarations tell the admin system what permissions exist. The admin system manages who has them. Modules never reference role names — only permission keys.

## Core Services

When `InitializeAsync` is called, the module receives an `ICoreServices` instance providing:

| Service | Purpose |
|---------|---------|
| `Logger` | `ILogger` scoped to the module's Id |
| `Configuration` | Read-only access to module-specific config (config-as-code) |
| `Identity` | Current user identity and claims |
| `Permissions` | Module-scoped `IPermissionContext` for capability checks |
| `NavigateTo(moduleId)` | Request shell navigation to another module |

## Module Isolation

Each module runs in its own `ModuleLoadContext` (extends `AssemblyLoadContext`), created with `isCollectible: true` to support future hot-reload.

### Shared Assembly Passthrough

Assemblies matching these prefixes are **not** loaded into the module's context — they fall through to the default context to ensure type identity across the module boundary:

- `ModularApp.Sdk`
- `ModularApp.Ui`
- `Avalonia`
- `CommunityToolkit.Mvvm`
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.DependencyInjection`
- `Semver`

All other assemblies are resolved by the module's own `AssemblyDependencyResolver`, allowing each module to carry its own version of any non-shared dependency.

## Version Gating

At discovery time, the loader instantiates the module temporarily to read `CompatibleEngineVersions`. If the declared range does not include the current engine version (from `ModularAppEngineVersion` in `Directory.Build.props`), the module is skipped with a warning log.

## Module Lifecycle

```
Discovered ──► Initialized ──► Active ──► Deactivated
                                │
                                ▼
                             Faulted
```

| State | Trigger |
|-------|---------|
| **Discovered** | `ModuleLoader.DiscoverModulesAsync` found and validated the module |
| **Initialized** | `ModuleHost.ActivateAsync` called `IModule.InitializeAsync` |
| **Active** | `CreateView()` succeeded and the view is displayed |
| **Deactivated** | `ModuleHost.DeactivateAsync` called `IModule.ShutdownAsync` |
| **Faulted** | Any exception during activation; shell shows `ModuleErrorView` with retry |

The `ModuleHost` caches the view returned by `CreateView()` — switching between modules in the nav does not re-create the view. `CreateView()` runs on the Avalonia UI thread via `Dispatcher.UIThread.InvokeAsync`.

## Directory Layout

Modules live in the `modules/` directory. Each module occupies a subdirectory named after the project. The loader scans for `ModularApp.Module.*.dll` within each subdirectory:

```
modules/
└── ModularApp.Module.Sample/
    ├── ModularApp.Module.Sample.dll    ← entry assembly (loader finds this)
    ├── Properties/
    │   └── AssemblyInfo.cs             ← ModuleMetadata + ModulePermission attributes
    ├── SampleModule.cs                 ← IModule implementation
    ├── ViewModels/
    ├── Views/
    └── config/                         ← module-specific config files
```

## Writing a New Module

1. Create a new Class Library project targeting `net10.0`.
2. Reference `ModularApp.Sdk` and `ModularApp.Ui` NuGet packages (or project references during development).
3. Implement `IModule` in a class named `{Feature}Module`.
4. Add `[assembly: ModuleMetadata(...)]` in `Properties/AssemblyInfo.cs`.
5. Optionally declare permissions via `[assembly: ModulePermission(...)]`.
6. Build and copy the output to `modules/{ProjectName}/` within the Workbench output directory.
