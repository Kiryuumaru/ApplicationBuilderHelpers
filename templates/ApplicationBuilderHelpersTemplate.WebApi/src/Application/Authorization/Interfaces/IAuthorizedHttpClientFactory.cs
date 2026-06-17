namespace Application.Authorization.Interfaces;

/// <summary>
/// Internal utility for creating HTTP clients with authorization.
/// Implemented by Application layer, callable by Application and Infrastructure.
/// </summary>
internal interface IAuthorizedHttpClientFactory : IDisposable
{
    Task<HttpClient> CreateAuthorizedAsync(
        string clientName,
        IEnumerable<string> permissionIdentifiers,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
