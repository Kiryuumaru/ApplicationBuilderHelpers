namespace Presentation.WebApp.Server.Models.Shared;

/// <summary>
/// Generic response wrapper for a list of items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public sealed class ListResponse<T>
{
    /// <summary>
    /// Gets or sets the list of items.
    /// </summary>
    public required IReadOnlyCollection<T> Items { get; init; }

    /// <summary>
    /// Creates a new list response from the specified items.
    /// </summary>
    /// <param name="items">The items to include in the response.</param>
    /// <returns>A new list response containing the items.</returns>
    public static ListResponse<T> From(IReadOnlyCollection<T> items) => new() { Items = items };

    /// <summary>
    /// Creates a new list response from the specified items.
    /// </summary>
    /// <param name="items">The items to include in the response.</param>
    /// <returns>A new list response containing the items.</returns>
    public static ListResponse<T> From(IEnumerable<T> items) => new() { Items = items.ToList() };
}
