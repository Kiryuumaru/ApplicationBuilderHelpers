namespace Infrastructure.EFCore.Identity.Models;

internal sealed class PasskeyChallengeEntity
{
    public required Guid Id { get; set; }
    public required byte[] Challenge { get; set; }
    public Guid? UserId { get; set; }
    public required int Type { get; set; }  // PasskeyChallengeType enum
    public required string OptionsJson { get; set; }
    public string? CredentialName { get; set; }  // For registration challenges
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}
