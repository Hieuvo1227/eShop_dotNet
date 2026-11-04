using eShop.Application.Catalog.DTOs;
using eShop.Application.Common.Interfaces;
using eShop.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Catalog.Queries.GetProductsWithPagination;

/// <summary>
/// Query lấy danh sách sản phẩm có phân trang, lọc theo Brand, Category, tìm kiếm từ khóa và sắp xếp.
/// </summary>
public record GetProductsWithPaginationQuery : IRequest<PaginatedList<ProductDto>>
{
    public int? BrandId { get; init; }
    public int? CategoryId { get; init; }
    public string? SearchTerm { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? SortBy { get; init; } // "price_asc", "price_desc", "name_desc", default: "name_asc"
    public int? PageIndex { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
}

public class GetProductsWithPaginationQueryHandler : IRequestHandler<GetProductsWithPaginationQuery, PaginatedList<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ProductDto>> Handle(
        GetProductsWithPaginationQuery request, 
        CancellationToken cancellationToken)
    {
        var pageIndex = request.PageIndex.GetValueOrDefault(1);
        var pageSize = request.PageSize.GetValueOrDefault(10);
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;

        // Sử dụng AsNoTracking() để tối ưu hiệu năng tối đa cho tác vụ chỉ đọc
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        // 1. Lọc theo thương hiệu
        if (request.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == request.BrandId.Value);
        }

        // 2. Lọc theo danh mục
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        // 3. Tìm kiếm theo từ khóa (Tên hoặc SKU)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.SKU.ToLower().Contains(term));
        }

        // 4. Lọc theo khoảng giá
        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        }

        // 5. Sắp xếp kết quả
        query = request.SortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_desc" => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        // 6. Chiếu (Project) sang DTO
        var projectedQuery = query.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            SKU = p.SKU,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            PictureUrl = p.PictureUrl,
            BrandId = p.BrandId,
            BrandName = p.Brand.Name,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name
        });

        // 7. Thực thi phân trang
        return await PaginatedList<ProductDto>.CreateAsync(
            projectedQuery, 
            pageIndex, 
            pageSize, 
            cancellationToken);
    }
}

