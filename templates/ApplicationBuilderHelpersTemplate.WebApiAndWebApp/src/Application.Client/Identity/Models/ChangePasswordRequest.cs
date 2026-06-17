namespace Application.Client.Identity.Models;

/// <summary>Change password request DTO.</summary>
public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
