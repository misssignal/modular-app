using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularApp.Sdk;
using ModularApp.Workbench.Services;
using ModularApp.Workbench.ViewModels;
using ModularApp.Workbench.Views;
using Serilog;

namespace ModularApp.Workbench;

public partial class App : Application
{
    private ServiceProvider _services = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            _services = ConfigureServices();

            // Global exception handling: don't crash the shell for module errors
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                Log.Fatal(args.ExceptionObject as Exception, "Unhandled domain exception");
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Log.Error(args.Exception, "Unobserved task exception");
                args.SetObserved();
            };

            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;

            // Check for existing setup profile
            var profileStore = _services.GetRequiredService<SetupProfileStore>();
            var existingProfile = profileStore.Load();

            if (existingProfile is not null)
            {
                // Existing profile — activate client and proceed to shell
                _ = ActivateAndShowShellAsync(mainWindow, existingProfile);
            }
            else
            {
                // First-time setup — show setup gate
                ShowSetupView(mainWindow);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Show the first-time setup view as the full window content.</summary>
    internal void ShowSetupView(Window mainWindow)
    {
        var config = _services!.GetRequiredService<IConfiguration>();

        // Load lab areas from configuration
        var labAreas = config.GetSection("LabAreas")
            .Get<List<LabAreaOption>>() ?? GetDefaultLabAreas();

        var setupVm = new SetupViewModel(
            _services.GetRequiredService<ICrateClient>(),
            _services.GetRequiredService<SetupProfileStore>(),
            labAreas,
            _services.GetRequiredService<ILogger<SetupViewModel>>());

        setupVm.SetupCompleted += profile =>
        {
            _ = ActivateAndShowShellAsync(mainWindow, profile);
        };

        var setupView = new SetupView { DataContext = setupVm };
        mainWindow.Content = setupView;
    }

    /// <summary>Activate the CRATE client and transition to the main shell.</summary>
    private async Task ActivateAndShowShellAsync(Window mainWindow, SetupProfile profile)
    {
        var crateClient = _services.GetRequiredService<ICrateClient>();

        if (!crateClient.IsActivated)
        {
            await crateClient.ActivateAsync(profile.Token);
        }

        // Set active labs on identity service
        var identityService = _services.GetRequiredService<IdentityService>();
        identityService.SetActiveLabs(profile.SelectedLabs.AsReadOnly());

        var mainVm = _services.GetRequiredService<MainWindowViewModel>();
        mainVm.SetProfile(profile);

        var navService = _services.GetRequiredService<INavigationService>();
        navService.SetViewModel(mainVm);

        // Shell settings
        var settingsVm = _services.GetRequiredService<ShellSettingsViewModel>();
        settingsVm.Refresh(profile);
        mainVm.SetSettingsViewModel(settingsVm);

        // Admin panel (only visible if user has admin labs)
        var permissionService = _services.GetRequiredService<IPermissionService>();
        var moduleRegistry = _services.GetRequiredService<IModuleRegistry>();
        var adminVm = new AdminViewModel(
            permissionService, moduleRegistry, profile.Username,
            _services.GetRequiredService<ILogger<AdminViewModel>>());
        mainVm.SetAdminViewModel(adminVm);

        mainWindow.DataContext = mainVm;
        mainWindow.Content = mainWindow.FindResource("ShellContent") ?? CreateShellContent();

        await mainVm.LoadModulesAsync();
    }

    /// <summary>Fallback: create the shell layout programmatically if not defined as a resource.</summary>
    private static object CreateShellContent()
    {
        // The MainWindow.axaml template binding handles this;
        // returning null lets the DataTemplate in MainWindow take over.
        return null!;
    }

    private static List<LabAreaOption> GetDefaultLabAreas() =>
    [
        new() { Name = "VSCL", ModuleSourceUri = "https://artifactory.example.com/crate-modules/vscl" },
        new() { Name = "Emissions", ModuleSourceUri = "https://artifactory.example.com/crate-modules/emissions" },
        new() { Name = "Battery", ModuleSourceUri = "https://artifactory.example.com/crate-modules/battery" },
        new() { Name = "BSL", ModuleSourceUri = "https://artifactory.example.com/crate-modules/bsl" },
        new() { Name = "Development", ModuleSourceUri = "./modules" },
    ];

    private static ServiceProvider ConfigureServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("Configuration/workbench.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Shell services
        services.AddSingleton<IdentityService>();
        services.AddSingleton<Sdk.IIdentityProvider>(sp => sp.GetRequiredService<IdentityService>());
        services.AddSingleton<IModuleLoader, ModuleLoader>();
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<ICoreServicesFactory, CoreServicesFactory>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<SetupProfileStore>();
        services.AddSingleton<ICrateClient, CrateClientService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ShellSettingsViewModel>();

        return services.BuildServiceProvider();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
