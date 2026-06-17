namespace Presentation.WebApi.Models.Shared;

/// <summary>
/// Generic response wrapper for a paginated list of items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public sealed class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the list of items for the current page.
    /// </summary>
    public required IReadOnlyCollection<T> Items { get; init; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Creates a new paged response from the specified items and total count.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="total">The total number of items.</param>
    /// <param name="page">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A new paged response.</returns>
    public static PagedResponse<T> From(IReadOnlyCollection<T> items, int total, int page = 1, int pageSize = 0) =>
        new()
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize > 0 ? pageSize : items.Count
        };
}
