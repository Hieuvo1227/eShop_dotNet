using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Common.Models;

/// <summary>
/// Cấu trúc phân trang danh sách chuẩn cho các câu truy vấn CQRS và API.
/// Hỗ trợ tuần tự hóa / giải tuần tự hóa JSON hai chiều với parameterless constructor.
/// </summary>
public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public int PageIndex { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// Parameterless constructor cần thiết cho System.Text.Json Deserializer ở phía Client.
    /// </summary>
    public PaginatedList() { }

    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}
