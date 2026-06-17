using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Requests;

/// <summary>
/// Request to get registration options for creating a new passkey.
/// </summary>
/// <param name="CredentialName">User-friendly name for this passkey (e.g., "My iPhone", "Work Laptop").</param>
public record PasskeyRegistrationOptionsRequest(
    [Required]
    [MaxLength(256)]
    string CredentialName
);
