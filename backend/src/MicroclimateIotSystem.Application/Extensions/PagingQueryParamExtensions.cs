using MicroclimateIotSystem.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Extensions;

public static class PagingQueryParamExtensions
{
    public static async Task<PaginatedResponse<T>> ToPaginatedResponseAsync<T>(this IQueryable<T> query, PagingQueryParams paging)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return PaginatedResponse<T>.SuccessOk(items, paging.Page, paging.PageSize, totalCount);
    }
}