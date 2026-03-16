using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ModularApp.Sdk;
using ModularApp.Workbench.Services;

namespace ModularApp.Workbench.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    private readonly ICrateClient _crateClient;
    private readonly SetupProfileStore _profileStore;
    private readonly ILogger<SetupViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompleteSetupCommand))]
    private string _token = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompleteSetupCommand))]
    private string _benchName = string.Empty;

    [ObservableProperty]
    private string _username = Environment.UserName;

    [ObservableProperty]
    private string _hostname = Environment.MachineName;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _selectedLabsSummary = string.Empty;

    /// <summary>All available lab areas from config.</summary>
    public ObservableCollection<LabAreaOption> LabAreas { get; } = new();

    /// <summary>Labs the user has selected (multi-select).</summary>
    public ObservableCollection<LabAreaOption> SelectedLabAreas { get; } = new();

    /// <summary>Raised when setup completes successfully.</summary>
    public event Action<SetupProfile>? SetupCompleted;

    public SetupViewModel(
        ICrateClient crateClient,
        SetupProfileStore profileStore,
        IEnumerable<LabAreaOption> labAreas,
        ILogger<SetupViewModel> logger)
    {
        _crateClient = crateClient;
        _profileStore = profileStore;
        _logger = logger;

        foreach (var area in labAreas)
            LabAreas.Add(area);

        SelectedLabAreas.CollectionChanged += OnSelectedLabsChanged;
    }

    private void OnSelectedLabsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SelectedLabsSummary = SelectedLabAreas.Count == 0
            ? string.Empty
            : string.Join(", ", SelectedLabAreas.Select(l => l.Name));
        CompleteSetupCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Toggle a lab's selection state.</summary>
    [RelayCommand]
    private void ToggleLab(LabAreaOption lab)
    {
        if (SelectedLabAreas.Contains(lab))
            SelectedLabAreas.Remove(lab);
        else
            SelectedLabAreas.Add(lab);
    }

    private bool CanCompleteSetup() =>
        !string.IsNullOrWhiteSpace(Token) &&
        !string.IsNullOrWhiteSpace(BenchName) &&
        SelectedLabAreas.Count > 0 &&
        !IsProcessing;

    [RelayCommand(CanExecute = nameof(CanCompleteSetup))]
    private async Task CompleteSetupAsync()
    {
        IsProcessing = true;
        ErrorMessage = string.Empty;

        try
        {
            await _crateClient.ActivateAsync(Token);

            var selectedLabs = SelectedLabAreas.Select(l => l.Name).ToList();
            var moduleSources = SelectedLabAreas
                .ToDictionary(l => l.Name, l => l.ModuleSourceUri);

            var profile = new SetupProfile
            {
                Token = Token,
                BenchName = BenchName,
                SelectedLabs = selectedLabs,
                ModuleSources = moduleSources,
                Username = Username,
                Hostname = Hostname,
            };

            _profileStore.Save(profile);

            // Register bench for each selected lab
            foreach (var lab in selectedLabs)
            {
                await _crateClient.RegisterBenchAsync(new BenchRegistration(
                    profile.BenchName, lab, profile.Username, profile.Hostname));
            }

            _logger.LogInformation(
                "First-time setup completed for bench {BenchName} in labs: {Labs}",
                profile.BenchName, string.Join(", ", selectedLabs));

            SetupCompleted?.Invoke(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Setup failed");
            ErrorMessage = $"Setup failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
