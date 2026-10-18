namespace ArdaNova.Application.Common.Results;

/// <summary>The one set of paging bounds every pager applies: page ≥ 1, 1 ≤ pageSize ≤ 100, and a skip that cannot overflow.</summary>
public static class PageRequest
{
    public const int MaximumPageSize = 100;

    public static int ClampPageSize(int pageSize) => Math.Clamp(pageSize, 1, MaximumPageSize);

    /// <summary>Clamps page so that (page - 1) * pageSize always fits in an int.</summary>
    public static int ClampPage(int page, int pageSize)
        => Math.Clamp(page, 1, int.MaxValue / ClampPageSize(pageSize));

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var size = ClampPageSize(pageSize);
        return (ClampPage(page, size), size);
    }
}
