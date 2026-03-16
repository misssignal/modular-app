using CommunityToolkit.Mvvm.ComponentModel;
using ModularApp.Sdk;

namespace ModularApp.Ui.ViewModels;

/// <summary>
/// Base ViewModel for module developers. Provides access to core services
/// and MVVM infrastructure via CommunityToolkit.
/// </summary>
public abstract class ModuleViewModelBase : ObservableObject
{
    public ICoreServices CoreServices { get; private set; } = null!;

    public void SetCoreServices(ICoreServices services) => CoreServices = services;
}
