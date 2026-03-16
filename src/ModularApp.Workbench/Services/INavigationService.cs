namespace ModularApp.Workbench.Services;

public interface INavigationService
{
    void NavigateTo(string moduleId);
    void SetViewModel(ViewModels.MainWindowViewModel viewModel);
}
