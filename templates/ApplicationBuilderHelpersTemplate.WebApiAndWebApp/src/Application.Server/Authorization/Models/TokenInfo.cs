using System.Text.Json.Nodes;

namespace Application.Server.Authorization.Models;

/// <summary>
/// Information about a decoded JWT token.
/// </summary>
public class TokenInfo
{
    /// <summary>
    /// Gets or sets the subject (sub) claim of the JWT token, typically representing the user identifier.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the issuer (iss) claim of the JWT token, identifying who issued the token.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Gets or sets the audience (aud) claim of the JWT token, identifying the intended recipient.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets the issued at (iat) claim of the JWT token, indicating when the token was issued.
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration time (exp) claim of the JWT token, indicating when the token expires.
    /// </summary>
    public DateTime Expires { get; set; }

    /// <summary>
    /// Gets or sets additional custom claims contained in the JWT token as key-value pairs.
    /// </summary>
    public Dictionary<string, JsonNode?> Claims { get; set; } = [];
}
