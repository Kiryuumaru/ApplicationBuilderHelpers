namespace Application.Client.Identity.Models;

/// <summary>Forgot password request DTO.</summary>
public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
