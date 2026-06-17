namespace Presentation.WebApp.Server.Models.Shared;

/// <summary>
/// Response wrapper for returning a GUID identifier.
/// </summary>
public sealed class IdResponse : IdResponse<Guid>
{
    /// <summary>
    /// Creates a new ID response from the specified GUID.
    /// </summary>
    /// <param name="id">The GUID identifier.</param>
    /// <returns>A new ID response containing the identifier.</returns>
    public static new IdResponse From(Guid id) => new() { Id = id };
}
