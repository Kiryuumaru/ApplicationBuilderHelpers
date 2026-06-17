using Application.Server.Identity.Models;
using Domain.Identity.Enums;

namespace Application.Server.Identity.Interfaces.Inbound;

/// <summary>
/// Service for handling OAuth authentication flows with external providers.
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Gets the list of configured OAuth providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of configured OAuth providers.</returns>
    Task<IReadOnlyCollection<OAuthProviderConfig>> GetProvidersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a specific provider is enabled.
    /// </summary>
    /// <param name="provider">The provider to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider is enabled.</returns>
    Task<bool> IsProviderEnabledAsync(ExternalLoginProvider provider, CancellationToken cancellationToken);

    /// <summary>
    /// Generates the authorization URL for initiating OAuth flow.
    /// </summary>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="redirectUri">The callback URI after authorization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authorization URL and state for the OAuth flow.</returns>
    Task<OAuthAuthorizationUrl> GetAuthorizationUrlAsync(
        ExternalLoginProvider provider,
        string redirectUri,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes the OAuth callback and exchanges the code for user info.
    /// </summary>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="code">The authorization code from the provider.</param>
    /// <param name="state">The state parameter to validate.</param>
    /// <param name="expectedState">The expected state value (from session).</param>
    /// <param name="redirectUri">The redirect URI used in the authorization request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing user info or error details.</returns>
    Task<OAuthCallbackResult> ProcessCallbackAsync(
        ExternalLoginProvider provider,
        string code,
        string state,
        string expectedState,
        string redirectUri,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes OAuth login - exchanges code for user info, finds or creates user.
    /// This encapsulates the full OAuth login flow including user lookup/creation.
    /// </summary>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="code">The authorization code from the provider.</param>
    /// <param name="state">The state parameter to validate.</param>
    /// <param name="expectedState">The expected state value (from session).</param>
    /// <param name="redirectUri">The redirect URI used in the authorization request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing user info (existing or newly created) or error details.</returns>
    Task<OAuthLoginResult> ProcessLoginAsync(
        ExternalLoginProvider provider,
        string code,
        string state,
        string expectedState,
        string redirectUri,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the state parameter to prevent CSRF attacks.
    /// </summary>
    /// <param name="state">The state from the callback.</param>
    /// <param name="expectedState">The expected state from the session.</param>
    /// <returns>True if the state is valid.</returns>
    bool ValidateState(string state, string expectedState);
}
