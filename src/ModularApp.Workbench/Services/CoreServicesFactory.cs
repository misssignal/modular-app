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
    private readonly IPermissionService _permissionService;

    public CoreServicesFactory(
        ILoggerFactory loggerFactory,
        Sdk.IIdentityProvider identityProvider,
        INavigationService navigationService,
        IPermissionService permissionService)
    {
        _loggerFactory = loggerFactory;
        _identityProvider = identityProvider;
        _navigationService = navigationService;
        _permissionService = permissionService;
    }

    public ICoreServices CreateForModule(string moduleId, string moduleDirectory)
    {
        var logger = _loggerFactory.CreateLogger(moduleId);
        var config = new ConfigurationService(moduleId, moduleDirectory);
        var permissions = _permissionService.CreateContextForModule(
            _identityProvider.Username, _identityProvider.ActiveLabs, moduleId);

        return new CoreServicesImpl(logger, config, _identityProvider, permissions, _navigationService);
    }

    private sealed class CoreServicesImpl : ICoreServices
    {
        public ILogger Logger { get; }
        public Sdk.IConfigurationProvider Configuration { get; }
        public Sdk.IIdentityProvider Identity { get; }
        public Sdk.IPermissionContext Permissions { get; }
        private readonly INavigationService _navigation;

        public CoreServicesImpl(
            ILogger logger,
            Sdk.IConfigurationProvider configuration,
            Sdk.IIdentityProvider identity,
            Sdk.IPermissionContext permissions,
            INavigationService navigation)
        {
            Logger = logger;
            Configuration = configuration;
            Identity = identity;
            Permissions = permissions;
            _navigation = navigation;
        }

        public void NavigateTo(string moduleId) => _navigation.NavigateTo(moduleId);
    }
}
