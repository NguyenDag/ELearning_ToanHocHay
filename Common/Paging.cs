using ELearning_ToanHocHay_Control.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Common
{
    /// <summary>P7 — bindable paging query (<c>?page=&amp;pageSize=&amp;search=</c>).</summary>
    public class PagedRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }

        public int NormPage => Math.Max(1, Page);
        public int NormPageSize => Math.Clamp(PageSize, 1, 100);
    }

    public static class PagingExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query, PagedRequest request)
        {
            var total = await query.CountAsync();
            var items = await query
                .Skip((request.NormPage - 1) * request.NormPageSize)
                .Take(request.NormPageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Total = total,
                Page = request.NormPage,
                PageSize = request.NormPageSize
            };
        }

        public static PagedResult<TOut> Map<TIn, TOut>(this PagedResult<TIn> src, Func<TIn, TOut> map) => new()
        {
            Items = src.Items.Select(map).ToList(),
            Total = src.Total,
            Page = src.Page,
            PageSize = src.PageSize
        };
    }
}
