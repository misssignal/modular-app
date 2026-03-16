namespace ModularApp.Workbench.ViewModels;

/// <summary>
/// Represents a module entry in the left navigation panel.
/// </summary>
public record ModuleNavItem(
    string Id,
    string Name,
    string IconKey,
    ModularApp.Sdk.PermissionLevel PermissionLevel = ModularApp.Sdk.PermissionLevel.View);
