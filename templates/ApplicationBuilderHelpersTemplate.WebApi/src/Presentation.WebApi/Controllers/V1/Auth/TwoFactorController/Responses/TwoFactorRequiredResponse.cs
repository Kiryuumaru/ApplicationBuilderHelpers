namespace Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Responses;

/// <summary>
/// Response model when login requires two-factor authentication.
/// </summary>
public sealed record TwoFactorRequiredResponse
{
    /// <summary>
    /// Indicates that two-factor authentication is required to complete login.
    /// </summary>
    public bool RequiresTwoFactor { get; init; } = true;

    /// <summary>
    /// The user ID to use when completing the 2FA login.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public string Message { get; init; } = "Two-factor authentication is required. Please provide your 2FA code.";
}
