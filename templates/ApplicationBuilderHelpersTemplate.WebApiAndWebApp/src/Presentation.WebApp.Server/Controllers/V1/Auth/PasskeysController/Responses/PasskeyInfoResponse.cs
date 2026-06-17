namespace Presentation.WebApp.Server.Controllers.V1.Auth.PasskeysController.Responses;

/// <summary>
/// Information about a registered passkey.
/// </summary>
/// <param name="Id">The unique identifier for this passkey.</param>
/// <param name="Name">The user-friendly name of this passkey.</param>
/// <param name="RegisteredAt">When this passkey was registered.</param>
/// <param name="LastUsedAt">When this passkey was last used for authentication.</param>
public record PasskeyInfoResponse(
    Guid Id,
    string Name,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastUsedAt
);
