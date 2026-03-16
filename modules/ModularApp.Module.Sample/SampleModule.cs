using Microsoft.Extensions.Logging;
using ModularApp.Module.Sample.ViewModels;
using ModularApp.Module.Sample.Views;
using ModularApp.Sdk;

namespace ModularApp.Module.Sample;

public class SampleModule : IModule
{
    private ICoreServices _services = null!;

    public string Id => "modularapp.module.sample";
    public string Name => "Sample Module";
    public string Version => "1.0.0";
    public string CompatibleEngineVersions => ">=1.0.0 <2.0.0";
    public string IconKey => "ClipboardTextClock";

    public Task InitializeAsync(ICoreServices coreServices)
    {
        _services = coreServices;
        _services.Logger.LogInformation("Sample module initialized");
        return Task.CompletedTask;
    }

    public object CreateView()
    {
        var vm = new SampleViewModel();
        vm.SetCoreServices(_services);
        return new SampleView { DataContext = vm };
    }

    public Task ShutdownAsync()
    {
        _services.Logger.LogInformation("Sample module shut down");
        return Task.CompletedTask;
    }
}
