[assembly: ModularApp.Sdk.ModuleMetadata(
    "modularapp.module.sample",
    "ModularApp.Module.Sample.SampleModule")]

// Permission declarations — what this module defines as capabilities
[assembly: ModularApp.Sdk.ModulePermission(
    "sample.view", "View sample module content")]
[assembly: ModularApp.Sdk.ModulePermission(
    "sample.edit", "Edit sample module data",
    ModularApp.Sdk.PermissionLevel.LimitedEdit)]
