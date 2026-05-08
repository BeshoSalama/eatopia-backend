namespace Eatopia.Application.Common;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    // Zero-based, kept for backward compatibility.
    public int PageIndex { get; set; }

    // One-based, easier for frontend query strings: ?page=1&pageSize=10
    public int Page => PageIndex + 1;

    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
