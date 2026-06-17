namespace Presentation.WebApp.Server.Models.Shared;

/// <summary>
/// Generic response wrapper for returning an identifier.
/// </summary>
/// <typeparam name="T">The type of the identifier.</typeparam>
public class IdResponse<T>
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public required T Id { get; init; }

    /// <summary>
    /// Creates a new ID response from the specified identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>A new ID response containing the identifier.</returns>
    public static IdResponse<T> From(T id) => new() { Id = id };
}
