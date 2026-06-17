namespace Application.AppEnvironment.Interfaces.Inbound;

/// <summary>
/// Provides application environment information.
/// </summary>
public interface IAppEnvironmentService
{
    /// <summary>
    /// Gets the current application environment configuration.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current app environment.</returns>
    Task<Domain.AppEnvironment.Models.AppEnvironment> GetEnvironment(CancellationToken cancellationToken = default);
}
