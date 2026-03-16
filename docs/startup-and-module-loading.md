# Startup & Module Loading

## Boot Sequence

The Workbench startup is orchestrated by `App.OnFrameworkInitializationCompleted` in `App.axaml.cs`.

### 1. Service Configuration

`App.ConfigureServices()` builds the DI container:

- Reads `Configuration/workbench.json` (optional, no hot-reload)
- Configures Serilog logging
- Registers all shell services as singletons
- Registers shell ViewModels

### 2. Global Exception Handlers

Before any UI is shown, the app installs two safety nets:

- `AppDomain.CurrentDomain.UnhandledException` — logs fatal errors without crashing the shell
- `TaskScheduler.UnobservedTaskException` — logs and marks observed to prevent process termination

These ensure module-thrown exceptions do not bring down the entire Workbench.

### 3. Setup Gate

```
SetupProfileStore.Load()
    │
    ├── null (first run) ──► ShowSetupView(mainWindow)
    │                              │
    │                         SetupViewModel collects:
    │                           - Auth token
    │                           - Bench name
    │                           - Lab selection (multi-select)
    │                              │
    │                         SetupCompleted event
    │                              │
    └── profile exists ──────────► ActivateAndShowShellAsync(mainWindow, profile)
```

The setup profile is persisted at `~/.crate/setup.json`. If the file is missing or invalid, the user sees a full-screen setup card. Once completed, the profile is saved and the shell activates.

### 4. Client Activation

`ActivateAndShowShellAsync` performs:

1. `ICrateClient.ActivateAsync(token)` — activates the CRATE backend client (stub for now)
2. `IdentityService.SetActiveLabs(profile.SelectedLabs)` — sets lab context
3. Creates and wires `MainWindowViewModel`, `ShellSettingsViewModel`, `AdminViewModel`
4. Sets `MainWindow.DataContext` and content to the shell layout

### 5. Module Discovery

`MainWindowViewModel.LoadModulesAsync()` runs after the shell is visible:

```
ModuleLoader.DiscoverModulesAsync()
    │
    ├── Scan modules/ directory for subdirectories
    │
    ├── For each subdirectory:
    │     ├── Find ModularApp.Module.*.dll
    │     ├── Create ModuleLoadContext (isolated ALC)
    │     ├── Load assembly, read ModuleMetadataAttribute
    │     ├── Instantiate module temporarily for version check
    │     ├── VersionCompatibility.IsCompatible(engine, module range)
    │     └── Return ModuleDiscoveryResult or skip
    │
    └── Return list of valid discoveries
```

### 6. Permission Gate

For each discovered module, the shell checks visibility:

```csharp
var effectivePermission = _permissionService.GetEffectivePermission(
    userId, activeLabs, result.ModuleId);

if (effectivePermission == PermissionLevel.Hidden)
    continue; // never appears in nav
```

Modules that pass the gate are registered in `ModuleRegistry` and added to the nav panel with their effective permission level.

### 7. Module Activation (On-Demand)

Modules are not initialized at startup — only when the user clicks them in the nav:

1. `ModuleHost.ActivateAsync()` loads the assembly from the module's `LoadContext`
2. Instantiates the `IModule` type via `Activator.CreateInstance`
3. Calls `InitializeAsync(ICoreServices)` with shell-provided services
4. Calls `CreateView()` on the UI thread via `Dispatcher.UIThread.InvokeAsync`
5. Caches the returned `Control` for subsequent navigation switches

If activation fails, a `ModuleErrorView` with a retry button is shown instead.

## Reset Shortcut

Pressing **Ctrl+Shift+Alt+F12** triggers a reset flow:

1. Confirmation dialog appears
2. If confirmed: `SetupProfileStore.Delete()` removes `~/.crate/setup.json`
3. `App.ShowSetupView(mainWindow)` replaces the shell with the setup gate
4. User must complete setup again to re-enter the shell

This is an escape hatch only — not exposed in any menu or settings panel.

## Configuration

The `Configuration/workbench.json` file provides:

- `Modules:Directory` — override the default modules scan path
- `LabAreas` — array of `{ Name, ModuleSourceUri }` options for setup

If the file is missing, hardcoded defaults in `App.GetDefaultLabAreas()` are used.
