namespace Application.Client.Identity.Models;

/// <summary>Reset user password request DTO (admin).</summary>
public sealed class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
