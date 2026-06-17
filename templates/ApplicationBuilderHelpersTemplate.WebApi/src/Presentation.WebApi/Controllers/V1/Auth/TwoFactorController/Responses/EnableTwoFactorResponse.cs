namespace Presentation.WebApi.Controllers.V1.Auth.TwoFactorController.Responses;

/// <summary>
/// Response model for successfully enabling two-factor authentication.
/// </summary>
public sealed record EnableTwoFactorResponse
{
    /// <summary>
    /// The recovery codes that can be used to access the account if the authenticator is lost.
    /// Each code can only be used once.
    /// </summary>
    public required IReadOnlyCollection<string> RecoveryCodes { get; init; }
}
