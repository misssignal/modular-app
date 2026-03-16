# Coding Standards

## Language & Framework

- **Target**: .NET 10.0, C# latest (`LangVersion latest`)
- **Nullable reference types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled

## Project Conventions

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Namespaces | Match directory structure | `ModularApp.Workbench.Services` |
| Interfaces | `I` prefix | `IModuleLoader`, `IPermissionService` |
| Module IDs | Lowercase dotted | `modularapp.module.sample` |
| Permission keys | `{module-short}.{action}` | `sample.edit`, `test-request.view` |
| Role IDs | Lowercase kebab-case | `lab-admin`, `module-admin` |
| ViewModels | `{Name}ViewModel` suffix | `MainWindowViewModel` |
| Views | `{Name}View` suffix | `SetupView`, `AdminView` |

### File Organization

- One type per file (exception: small records colocated with their consuming interface)
- Interfaces in their own file (`IPermissionService.cs`)
- Related records at the bottom of the interface file (e.g., `LabRole`, `LabMember`)
- Assembly attributes in `Properties/AssemblyInfo.cs`

### MVVM Patterns

- Use `CommunityToolkit.Mvvm` source generators: `[ObservableProperty]`, `[RelayCommand]`
- ViewModels inherit from `ViewModelBase` (which extends `ObservableObject`)
- Views set `DataContext` in code-behind or at construction time
- Prefer `x:DataType` in AXAML for compile-time binding validation

### Dependency Injection

- All shell services registered as singletons in `App.ConfigureServices()`
- Modules do not register services — they consume `ICoreServices`
- Use constructor injection; avoid service locator patterns
- Factory pattern for module-scoped services (`ICoreServicesFactory`)

### Permission Checks

- **Always** check capabilities via `IPermissionContext.Can(permissionKey)` or `EffectiveLevel`
- **Never** check role names in application logic
- Gate module visibility at the shell level using `GetEffectivePermission`
- Gate UI elements within modules using `IPermissionContext`

### Error Handling

- Shell installs global exception handlers to prevent module crashes from killing the app
- Module activation failures result in `ModuleErrorView` with retry, not app termination
- Use structured logging (`_logger.LogError(ex, "message {Param}", value)`)
- Async exceptions logged via `TaskScheduler.UnobservedTaskException`

### Configuration

- Config is read-only JSON shipped with the app (config-as-code)
- Modules receive config via `ICoreServices.Configuration`
- No runtime config writes — module settings that need persistence should use their own mechanism within their module directory

## SDK Constraints

The `ModularApp.Sdk` package must remain **Avalonia-free**:

- `IModule.CreateView()` returns `object`, not `Control`
- Logging abstractions only (`Microsoft.Extensions.Logging.Abstractions`)
- No UI dependencies in the SDK project

## Module Development

- Reference `ModularApp.Sdk` and `ModularApp.Ui` only
- Declare metadata via `[assembly: ModuleMetadata(...)]`
- Declare permissions via `[assembly: ModulePermission(...)]`
- Declare compatible engine versions in `IModule.CompatibleEngineVersions`
- Keep module-specific dependencies isolated — they load into a separate `AssemblyLoadContext`

## Logging

- Serilog with file sink configured at shell level
- Modules receive `ILogger` scoped to their module ID
- Use structured logging with named parameters
- Log levels: `Debug` for discovery details, `Information` for lifecycle events, `Warning` for skipped modules, `Error` for failures, `Fatal` for unrecoverable shell errors
