namespace Presentation.WebApi.Controllers.V1.Auth.Shared.Responses;

/// <summary>
/// Response model for successful authentication.
/// </summary>
public sealed record AuthResponse
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// The token type (always "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Access token expiration time in seconds.
    /// </summary>
    public required int ExpiresIn { get; init; }

    /// <summary>
    /// The authenticated user's information.
    /// </summary>
    public required UserInfo User { get; init; }
}
