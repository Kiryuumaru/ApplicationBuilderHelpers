namespace Application.Client.Identity.Models;

/// <summary>Create API key request DTO.</summary>
public sealed class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
}
