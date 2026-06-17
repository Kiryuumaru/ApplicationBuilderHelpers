namespace Application.Client.Shared.Models;

/// <summary>Error response from API.</summary>
public sealed class ErrorResponse
{
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? Title { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }
}
