# ModularApp.Module.Sample

Reference implementation of a CRATE Workbench module. Use this as a template when building new modules.

## Module Identity

| Field | Value |
|-------|-------|
| Module ID | `modularapp.module.sample` |
| Display Name | Sample Module |
| Version | 1.0.0 |
| Compatible Engine | `>=1.0.0 <2.0.0` |

## Declared Permissions

| Key | Description | Minimum Level |
|-----|-------------|---------------|
| `sample.view` | View sample module content | View |
| `sample.edit` | Edit sample module data | LimitedEdit |

## Structure

```
ModularApp.Module.Sample/
├── SampleModule.cs            ← IModule implementation (entry point)
├── Properties/
│   └── AssemblyInfo.cs        ← ModuleMetadata + ModulePermission attributes
├── ViewModels/
│   └── SampleViewModel.cs     ← Module ViewModel
├── Views/
│   └── SampleView.axaml       ← Module UI
└── config/                    ← Module-specific configuration
```

## How It Works

1. The shell discovers this module by finding `ModularApp.Module.Sample.dll` in the `modules/` directory.
2. `ModuleMetadataAttribute` tells the loader the module ID and entry type.
3. The loader checks `CompatibleEngineVersions` against the current engine version.
4. When the user clicks the module in the nav, `SampleModule.InitializeAsync` is called with `ICoreServices`.
5. `CreateView()` returns a `SampleView` bound to a `SampleViewModel`.

## Using as a Template

1. Copy this directory and rename to `ModularApp.Module.{YourFeature}`
2. Update `AssemblyInfo.cs` with your module ID and type
3. Rename `SampleModule` to `{YourFeature}Module` and update all properties
4. Declare your permissions in `AssemblyInfo.cs`
5. Build and deploy to the `modules/` directory
