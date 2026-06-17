namespace Presentation.WebApi.Controllers.V1.Auth.OAuthController.Responses;

/// <summary>
/// Response containing list of OAuth providers.
/// </summary>
public sealed record OAuthProvidersResponse
{
    /// <summary>
    /// List of available OAuth providers.
    /// </summary>
    public required IReadOnlyCollection<OAuthProviderResponse> Providers { get; init; }
}
