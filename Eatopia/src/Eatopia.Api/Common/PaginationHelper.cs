using Eatopia.Application.Common;

namespace Eatopia.Api.Common;

public static class PaginationHelper
{
    public static int ToPageIndex(int page, int? pageIndex = null)
    {
        return pageIndex ?? Math.Max(page - 1, 0);
    }

    public static object ToMeta<T>(PagedResult<T> result)
    {
        return new
        {
            result.Page,
            result.PageIndex,
            result.PageSize,
            result.TotalCount,
            result.TotalPages
        };
    }
}
