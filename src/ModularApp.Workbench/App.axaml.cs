using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularApp.Workbench.Services;
using ModularApp.Workbench.ViewModels;
using ModularApp.Workbench.Views;
using Serilog;

namespace ModularApp.Workbench;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var services = ConfigureServices();

            var mainVm = services.GetRequiredService<MainWindowViewModel>();

            // Wire navigation service to the main ViewModel
            var navService = services.GetRequiredService<INavigationService>();
            navService.SetViewModel(mainVm);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };

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

            // Load modules after the window is set up
            _ = mainVm.LoadModulesAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

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
        services.AddSingleton<Sdk.IIdentityProvider, IdentityService>();
        services.AddSingleton<IModuleLoader, ModuleLoader>();
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();
        services.AddSingleton<ICoreServicesFactory, CoreServicesFactory>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

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
