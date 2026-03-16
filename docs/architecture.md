# CRATE Workbench — Architecture Overview

## Purpose

CRATE Workbench is a cross-platform desktop application that serves as a modular shell for GM lab test management. It provides a unified interface where individual labs (VSCL, Emissions, Battery, BSL, etc.) load their own feature modules at runtime without recompiling or redeploying the shell.

## Technology Stack

| Layer | Technology |
|-------|-----------|
| UI framework | Avalonia 11.3 with Fluent theme |
| Target runtime | .NET 10.0 |
| MVVM toolkit | CommunityToolkit.Mvvm 8.2 |
| Logging | Serilog (file sink) |
| DI container | Microsoft.Extensions.DependencyInjection |
| Configuration | Microsoft.Extensions.Configuration (JSON) |
| Module isolation | System.Runtime.Loader.AssemblyLoadContext |
| Version gating | Semver NuGet package |

## High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│                   CRATE Workbench                    │
│  ┌──────────────────────────────────────────────┐   │
│  │              Avalonia Shell UI                │   │
│  │  ┌──────┐  ┌──────────────────────────────┐  │   │
│  │  │ Nav  │  │   Content Panel              │  │   │
│  │  │ Panel│  │   (active module / settings   │  │   │
│  │  │      │  │    / admin view)              │  │   │
│  │  └──────┘  └──────────────────────────────┘  │   │
│  └──────────────────────────────────────────────┘   │
│                                                     │
│  ┌──────────────────────────────────────────────┐   │
│  │             Shell Services                    │   │
│  │  Identity · Permissions · Navigation          │   │
│  │  ModuleLoader · ModuleRegistry · Config       │   │
│  │  CrateClient · SetupProfileStore              │   │
│  └──────────────────────────────────────────────┘   │
│                                                     │
│  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐   │
│  │Module A│  │Module B│  │Module C│  │Module D│   │
│  │(ALC)   │  │(ALC)   │  │(ALC)   │  │(ALC)   │   │
│  └────────┘  └────────┘  └────────┘  └────────┘   │
└─────────────────────────────────────────────────────┘
        ▲               ▲
        │               │
  ┌─────┴──────┐  ┌─────┴──────┐
  │ModularApp  │  │ModularApp  │
  │   .Sdk     │  │   .Ui      │
  │ (shared)   │  │ (shared)   │
  └────────────┘  └────────────┘
```

## Project Structure

```
ModularApp/
├── src/
│   ├── ModularApp.Sdk/           # Shared contracts — referenced by all modules
│   ├── ModularApp.Ui/            # Shared UI primitives and theme resources
│   └── ModularApp.Workbench/     # Shell application (entry point)
│       ├── Configuration/        # workbench.json
│       ├── Services/             # Shell services (loader, permissions, identity)
│       ├── ViewModels/           # Shell view models (MVVM)
│       └── Views/                # Shell Avalonia views (.axaml)
├── modules/
│   └── ModularApp.Module.Sample/ # Reference module implementation
├── docs/                         # Project documentation
│   ├── adr/                      # Architecture Decision Records
│   └── schema/                   # Database schema references
└── Directory.Build.props         # Shared MSBuild properties
```

## Key Design Principles

1. **Module isolation** — Each module loads into its own `AssemblyLoadContext`. Shared assemblies (SDK, UI, Avalonia, CommunityToolkit) pass through to the default context to preserve type identity.

2. **Capability-based permissions** — Access control checks permission keys (e.g., `sample.edit`), never role names. Roles are a grouping mechanism for permissions, scoped per lab.

3. **Lab-scoped everything** — Roles, module access, and permissions are all scoped to a lab. Users may belong to multiple labs; the effective permission is the maximum across all active labs.

4. **Config-as-code** — Configuration is read-only JSON shipped with the application. Modules cannot write to configuration at runtime.

5. **Shell owns the frame** — Navigation, settings, admin, and module lifecycle are shell concerns. Modules own only the content they render in the right panel.

## Data Flow

1. **Startup**: `App.OnFrameworkInitializationCompleted` → check `SetupProfileStore` → show setup gate or activate shell.
2. **Module discovery**: `ModuleLoader.DiscoverModulesAsync` → scan `/modules` directory → load assemblies → version gate → return `ModuleDiscoveryResult` list.
3. **Permission gate**: `MainWindowViewModel.LoadModulesAsync` → for each discovered module, check `PermissionService.GetEffectivePermission` → skip if `Hidden`.
4. **Module activation**: User selects module in nav → `ModuleHost.ActivateAsync` → instantiate `IModule`, call `InitializeAsync(ICoreServices)`, call `CreateView()` on UI thread.
5. **Permission checks in modules**: Module calls `coreServices.Permissions.Can("sample.edit")` → returns `bool` based on granted permissions for current user across active labs.

## Dependency Injection Registration

All shell services are registered as singletons in `App.ConfigureServices()`:

- `IConfiguration` — from `workbench.json`
- `IdentityService` (also as `IIdentityProvider`)
- `IModuleLoader` → `ModuleLoader`
- `IModuleRegistry` → `ModuleRegistry`
- `IPermissionService` → `PermissionService`
- `ICoreServicesFactory` → `CoreServicesFactory`
- `INavigationService` → `NavigationService`
- `SetupProfileStore`
- `ICrateClient` → `CrateClientService`
- `MainWindowViewModel`, `ShellSettingsViewModel`
