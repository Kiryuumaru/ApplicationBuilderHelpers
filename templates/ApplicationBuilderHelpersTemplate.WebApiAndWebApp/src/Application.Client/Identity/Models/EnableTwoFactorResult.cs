namespace Application.Client.Identity.Models;

/// <summary>
/// Represents the result of enabling 2FA.
/// </summary>
public class EnableTwoFactorResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the recovery codes (returned when 2FA is enabled).
    /// </summary>
    public List<string> RecoveryCodes { get; set; } = new();

    public static EnableTwoFactorResult Succeeded(List<string> recoveryCodes) => new()
    {
        Success = true,
        RecoveryCodes = recoveryCodes
    };

    public static EnableTwoFactorResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
