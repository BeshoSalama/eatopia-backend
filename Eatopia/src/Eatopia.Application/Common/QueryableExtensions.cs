using Microsoft.EntityFrameworkCore;

namespace Eatopia.Application.Common;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageIndex,
        int pageSize)
    {
        pageIndex = Math.Max(pageIndex, 0);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var total = await query.CountAsync();

        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}
