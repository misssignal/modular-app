# ModularApp.Workbench

The CRATE Workbench shell application — the main entry point and host for all modules.

## Responsibilities

- First-time setup gate (token, bench name, lab selection)
- Module discovery, version gating, and lifecycle management
- Permission-gated module loading (hidden modules never appear)
- Shell UI: navigation panel, content area, status bar
- Admin panel for lab member/role/permission management
- Shell settings panel (profile, modules, logging, about)
- Global exception handling to prevent module crashes from killing the shell

## Project Layout

```
ModularApp.Workbench/
├── App.axaml / App.axaml.cs       ← Application entry, DI setup, startup flow
├── Configuration/
│   └── workbench.json             ← Lab areas, module directory config
├── Services/
│   ├── ModuleLoader.cs            ← Module discovery and version gating
│   ├── ModuleLoadContext.cs       ← Isolated AssemblyLoadContext per module
│   ├── ModuleHost.cs              ← Module lifecycle (init → active → deactivated)
│   ├── ModuleRegistry.cs          ← Registry of loaded ModuleHost instances
│   ├── PermissionService.cs       ← In-memory permission stub (lab-scoped RBAC)
│   ├── IdentityService.cs         ← AD/LDAP identity + active labs
│   ├── CoreServicesFactory.cs     ← Creates module-scoped ICoreServices
│   ├── SetupProfileStore.cs       ← Reads/writes ~/.crate/setup.json
│   ├── CrateClientService.cs      ← Stub ICrateClient implementation
│   └── NavigationService.cs       ← Cross-module navigation
├── ViewModels/
│   ├── MainWindowViewModel.cs     ← Shell ViewModel (modules, settings, admin)
│   ├── SetupViewModel.cs          ← First-time setup flow
│   ├── ShellSettingsViewModel.cs  ← Gear-icon settings panel
│   ├── AdminViewModel.cs          ← Lab admin panel
│   └── ModuleNavItem.cs           ← Nav entry with permission level
└── Views/
    ├── MainWindow.axaml           ← Shell layout (nav + content + status)
    ├── SetupView.axaml            ← First-run setup screen
    ├── ShellSettingsView.axaml    ← Settings panel
    ├── AdminView.axaml            ← Admin panel
    └── ModuleErrorView.cs         ← Error display with retry
```

## Running

```bash
dotnet run --project src/ModularApp.Workbench
```

On first run, the setup screen collects an auth token, bench name, and lab selection. The profile is saved to `~/.crate/setup.json`. Subsequent runs skip setup and load directly into the shell.

## Reset

Press **Ctrl+Shift+Alt+F12** to delete the setup profile and force re-setup.
