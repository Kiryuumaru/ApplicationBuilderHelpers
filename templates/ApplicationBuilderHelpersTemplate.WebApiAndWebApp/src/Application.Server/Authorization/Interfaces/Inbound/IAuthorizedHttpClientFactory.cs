namespace Application.Server.Authorization.Interfaces.Inbound;

/// <summary>
/// Internal utility for Application services to create HTTP clients with authorization.
/// Implemented by Application layer services, not Infrastructure.
/// </summary>
public interface IAuthorizedHttpClientFactory : IDisposable
{
    Task<HttpClient> CreateAuthorizedAsync(
        string clientName,
        IEnumerable<string> permissionIdentifiers,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
