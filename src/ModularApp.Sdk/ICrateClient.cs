namespace ModularApp.Sdk;

/// <summary>
/// Client interface for CRATE backend services.
/// Activated with an auth token during first-time setup;
/// provides access to bench registration and future data operations.
/// </summary>
public interface ICrateClient
{
    /// <summary>Whether the client has been activated with a valid token.</summary>
    bool IsActivated { get; }

    /// <summary>
    /// Activate the client with an authorization token.
    /// Must be called before any other operations.
    /// </summary>
    Task ActivateAsync(string token);

    /// <summary>
    /// Register or update bench/workstation information in the CRATE DB.
    /// </summary>
    Task RegisterBenchAsync(BenchRegistration registration);
}

/// <summary>
/// Information about a bench/workstation to register with CRATE.
/// </summary>
public record BenchRegistration(
    string BenchName,
    string LabArea,
    string Username,
    string Hostname);
