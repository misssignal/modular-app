using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ModularApp.Sdk;
using ModularApp.Workbench.Services;

namespace ModularApp.Workbench.ViewModels;

/// <summary>
/// ViewModel for the centralized Admin panel.
/// Lab-scoped: only shows admin controls for labs where the user has LabAdmin permission.
/// </summary>
public partial class AdminViewModel : ViewModelBase
{
    private readonly IPermissionService _permissionService;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ILogger<AdminViewModel> _logger;
    private readonly string _currentUserId;

    [ObservableProperty]
    private ObservableCollection<string> _adminLabs = new();

    [ObservableProperty]
    private string? _selectedLab;

    [ObservableProperty]
    private ObservableCollection<LabMemberDisplay> _labMembers = new();

    [ObservableProperty]
    private ObservableCollection<LabRoleDisplay> _labRoles = new();

    [ObservableProperty]
    private ObservableCollection<ModulePermissionDisplay> _modulePermissions = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Add member fields
    [ObservableProperty]
    private string _newMemberId = string.Empty;

    // Add role fields
    [ObservableProperty]
    private string _newRoleId = string.Empty;

    [ObservableProperty]
    private string _newRoleDisplayName = string.Empty;

    [ObservableProperty]
    private PermissionLevel _newRoleLevel = PermissionLevel.View;

    public PermissionLevel[] AvailablePermissionLevels { get; } =
        Enum.GetValues<PermissionLevel>();

    public AdminViewModel(
        IPermissionService permissionService,
        IModuleRegistry moduleRegistry,
        string currentUserId,
        ILogger<AdminViewModel> logger)
    {
        _permissionService = permissionService;
        _moduleRegistry = moduleRegistry;
        _currentUserId = currentUserId;
        _logger = logger;
    }

    /// <summary>Load the list of labs this user can administer.</summary>
    public void Refresh()
    {
        AdminLabs.Clear();
        foreach (var lab in _permissionService.GetAdminLabs(_currentUserId))
            AdminLabs.Add(lab);

        if (AdminLabs.Count > 0 && SelectedLab is null)
            SelectedLab = AdminLabs[0];
    }

    partial void OnSelectedLabChanged(string? value)
    {
        if (value is not null)
            LoadLabDetails(value);
    }

    private void LoadLabDetails(string labId)
    {
        // Members
        LabMembers.Clear();
        foreach (var member in _permissionService.GetLabMembers(labId))
        {
            LabMembers.Add(new LabMemberDisplay(
                member.UserId,
                string.Join(", ", member.RoleIds)));
        }

        // Roles
        LabRoles.Clear();
        foreach (var role in _permissionService.GetLabRoles(labId))
        {
            var perms = _permissionService.GetRolePermissions(role.RoleId, labId);
            LabRoles.Add(new LabRoleDisplay(
                role.RoleId,
                role.DisplayName,
                role.Level,
                perms.Select(p => p.PermissionKey).ToList()));
        }

        // Module permissions (from loaded module assemblies)
        ModulePermissions.Clear();
        foreach (var host in _moduleRegistry.GetAllHosts())
        {
            ModulePermissions.Add(new ModulePermissionDisplay(
                host.ModuleId,
                host.ModuleName));
        }
    }

    [RelayCommand]
    private void AddMember()
    {
        if (string.IsNullOrWhiteSpace(NewMemberId) || SelectedLab is null) return;

        _permissionService.AddLabMember(NewMemberId.Trim(), SelectedLab);
        StatusMessage = $"Added {NewMemberId.Trim()} to {SelectedLab}";
        NewMemberId = string.Empty;
        LoadLabDetails(SelectedLab);
    }

    [RelayCommand]
    private void RemoveMember(LabMemberDisplay member)
    {
        if (SelectedLab is null) return;

        _permissionService.RemoveLabMember(member.UserId, SelectedLab);
        StatusMessage = $"Removed {member.UserId} from {SelectedLab}";
        LoadLabDetails(SelectedLab);
    }

    [RelayCommand]
    private void CreateRole()
    {
        if (string.IsNullOrWhiteSpace(NewRoleId) ||
            string.IsNullOrWhiteSpace(NewRoleDisplayName) ||
            SelectedLab is null) return;

        _permissionService.CreateRole(
            NewRoleId.Trim(), SelectedLab, NewRoleDisplayName.Trim(), NewRoleLevel);
        StatusMessage = $"Created role {NewRoleDisplayName.Trim()} in {SelectedLab}";
        NewRoleId = string.Empty;
        NewRoleDisplayName = string.Empty;
        LoadLabDetails(SelectedLab);
    }

    [RelayCommand]
    private void AssignRole(string parameter)
    {
        // parameter = "userId|roleId"
        if (SelectedLab is null) return;
        var parts = parameter.Split('|');
        if (parts.Length != 2) return;

        _permissionService.AssignRole(parts[0], SelectedLab, parts[1]);
        StatusMessage = $"Assigned {parts[1]} to {parts[0]} in {SelectedLab}";
        LoadLabDetails(SelectedLab);
    }
}

public record LabMemberDisplay(string UserId, string Roles);
public record LabRoleDisplay(string RoleId, string DisplayName, PermissionLevel Level, List<string> Permissions);
public record ModulePermissionDisplay(string ModuleId, string ModuleName);
