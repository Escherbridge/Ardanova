namespace ArdaNova.Application.Common.Results;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Normalises page and pageSize, so TotalPages never divides by zero and HasNextPage always terminates.</summary>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = Math.Max(0, totalCount);
        PageSize = Math.Max(1, pageSize);
        Page = Math.Max(1, page);
    }

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new(Array.Empty<T>(), 0, page, pageSize);

    public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        new(Items.Select(mapper).ToList(), TotalCount, Page, PageSize);
}
