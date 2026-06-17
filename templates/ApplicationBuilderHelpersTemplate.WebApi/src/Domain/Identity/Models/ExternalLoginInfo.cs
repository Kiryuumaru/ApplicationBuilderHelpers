namespace Domain.Identity.Models;

/// <summary>
/// Information about a user's linked external login provider.
/// </summary>
public sealed record ExternalLoginInfo
{
    public required string Provider { get; init; }
    public required string ProviderSubject { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public required DateTimeOffset LinkedAt { get; init; }
}
