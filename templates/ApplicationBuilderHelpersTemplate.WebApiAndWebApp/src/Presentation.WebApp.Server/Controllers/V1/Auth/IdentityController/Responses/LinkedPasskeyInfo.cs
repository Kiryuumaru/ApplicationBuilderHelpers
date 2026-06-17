namespace Presentation.WebApp.Server.Controllers.V1.Auth.IdentityController.Responses;

/// <summary>
/// Information about a linked passkey.
/// </summary>
public sealed record LinkedPasskeyInfo
{
    /// <summary>
    /// The passkey's unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The passkey's display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// When the passkey was registered.
    /// </summary>
    public required DateTimeOffset RegisteredAt { get; init; }
}
