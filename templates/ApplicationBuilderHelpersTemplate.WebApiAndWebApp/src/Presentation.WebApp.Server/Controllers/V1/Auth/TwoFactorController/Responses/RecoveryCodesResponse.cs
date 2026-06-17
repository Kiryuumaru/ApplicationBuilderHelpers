namespace Presentation.WebApp.Server.Controllers.V1.Auth.TwoFactorController.Responses;

/// <summary>
/// Response containing newly generated recovery codes.
/// </summary>
/// <param name="RecoveryCodes">The list of recovery codes (10 codes).</param>
/// <param name="Message">A message explaining how to use the codes.</param>
public record RecoveryCodesResponse(
    IReadOnlyCollection<string> RecoveryCodes,
    string Message = "Store these recovery codes in a safe place. Each code can only be used once.");
