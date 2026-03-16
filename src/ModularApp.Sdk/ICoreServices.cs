using Microsoft.Extensions.Logging;

namespace ModularApp.Sdk;

/// <summary>
/// Services provided by the shell to each module.
/// Modules consume these services but do not extend or register new ones.
/// </summary>
public interface ICoreServices
{
    /// <summary>Logger scoped to this module's Id.</summary>
    ILogger Logger { get; }

    /// <summary>Read-only access to module configuration (config-as-code).</summary>
    IConfigurationProvider Configuration { get; }

    /// <summary>Identity and claims for the current user session.</summary>
    IIdentityProvider Identity { get; }

    /// <summary>Request the shell navigate to a different module by Id.</summary>
    void NavigateTo(string moduleId);
}
