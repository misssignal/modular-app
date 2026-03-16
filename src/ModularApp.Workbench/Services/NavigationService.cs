using ModularApp.Workbench.ViewModels;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Allows modules to request navigation to other modules via the shell.
/// </summary>
public class NavigationService : INavigationService
{
    private MainWindowViewModel? _viewModel;

    public void SetViewModel(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void NavigateTo(string moduleId)
    {
        _viewModel?.NavigateToModule(moduleId);
    }
}
