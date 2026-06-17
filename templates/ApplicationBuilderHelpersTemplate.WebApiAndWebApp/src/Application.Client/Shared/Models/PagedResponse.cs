namespace Application.Client.Shared.Models;

/// <summary>Generic paged response wrapper.</summary>
public sealed class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
