using Microsoft.Extensions.Logging;
using ModularApp.Sdk;

namespace ModularApp.Workbench.Services;

/// <summary>
/// Stub implementation of ICrateClient.
/// Accepts any token and logs registration calls.
/// Will be replaced with real CRATE backend integration.
/// </summary>
public sealed class CrateClientService : ICrateClient
{
    private readonly ILogger<CrateClientService> _logger;
    private string? _token;

    public CrateClientService(ILogger<CrateClientService> logger)
    {
        _logger = logger;
    }

    public bool IsActivated => _token is not null;

    public Task ActivateAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.", nameof(token));

        _token = token;
        _logger.LogInformation("CRATE client activated (token length: {Length})", token.Length);
        return Task.CompletedTask;
    }

    public Task RegisterBenchAsync(BenchRegistration registration)
    {
        if (!IsActivated)
            throw new InvalidOperationException("Client must be activated before registering a bench.");

        _logger.LogInformation(
            "Bench registration: {BenchName} in {LabArea} by {Username}@{Hostname}",
            registration.BenchName,
            registration.LabArea,
            registration.Username,
            registration.Hostname);

        // Stub: in the future this will POST to CRATE DB
        return Task.CompletedTask;
    }
}
