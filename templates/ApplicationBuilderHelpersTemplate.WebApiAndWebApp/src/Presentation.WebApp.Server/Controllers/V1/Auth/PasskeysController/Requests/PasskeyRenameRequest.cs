using System.ComponentModel.DataAnnotations;

namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Requests;

/// <summary>
/// Request to rename a passkey.
/// </summary>
/// <param name="Name">The new name for the passkey.</param>
public record PasskeyRenameRequest(
    [Required]
    [MaxLength(256)]
    string Name
);
