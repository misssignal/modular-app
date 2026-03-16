using Microsoft.Extensions.Logging;
using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Creates a scoped ICoreServices instance for each module,
/// composing module-specific logger, config, shared identity, and navigation.
/// </summary>
public class CoreServicesFactory : ICoreServicesFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Sdk.IIdentityProvider _identityProvider;
    private readonly INavigationService _navigationService;

    public CoreServicesFactory(
        ILoggerFactory loggerFactory,
        Sdk.IIdentityProvider identityProvider,
        INavigationService navigationService)
    {
        _loggerFactory = loggerFactory;
        _identityProvider = identityProvider;
        _navigationService = navigationService;
    }

    public ICoreServices CreateForModule(string moduleId, string moduleDirectory)
    {
        var logger = _loggerFactory.CreateLogger(moduleId);
        var config = new ConfigurationService(moduleId, moduleDirectory);

        return new CoreServicesImpl(logger, config, _identityProvider, _navigationService);
    }

    private sealed class CoreServicesImpl : ICoreServices
    {
        public ILogger Logger { get; }
        public Sdk.IConfigurationProvider Configuration { get; }
        public Sdk.IIdentityProvider Identity { get; }
        private readonly INavigationService _navigation;

        public CoreServicesImpl(
            ILogger logger,
            Sdk.IConfigurationProvider configuration,
            Sdk.IIdentityProvider identity,
            INavigationService navigation)
        {
            Logger = logger;
            Configuration = configuration;
            Identity = identity;
            _navigation = navigation;
        }

        public void NavigateTo(string moduleId) => _navigation.NavigateTo(moduleId);
    }
}
