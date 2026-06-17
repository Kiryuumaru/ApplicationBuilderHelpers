namespace Application.AppEnvironment.Interfaces.Inbound;

/// <summary>
/// Application service for resolving the current application environment.
/// </summary>
public interface IAppEnvironmentService
{
    Task<Domain.AppEnvironment.Models.AppEnvironment> GetEnvironment(CancellationToken cancellationToken = default);
}
