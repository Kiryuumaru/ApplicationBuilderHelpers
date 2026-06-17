namespace Application.Client.Shared.Models;

/// <summary>Generic list response wrapper.</summary>
public sealed class ListResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
