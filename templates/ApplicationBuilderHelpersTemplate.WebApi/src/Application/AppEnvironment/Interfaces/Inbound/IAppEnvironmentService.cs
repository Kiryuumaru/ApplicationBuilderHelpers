namespace Application.AppEnvironment.Interfaces.Inbound;

/// <summary>
/// Service for retrieving application environment information.
/// </summary>
public interface IAppEnvironmentService
{
    /// <summary>
    /// Gets the current application environment configuration.
    /// </summary>
    Task<Domain.AppEnvironment.Models.AppEnvironment> GetEnvironment(CancellationToken cancellationToken = default);
}
