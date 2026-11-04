using System.ComponentModel;
using eShop.Application.Catalog.DTOs;
using eShop.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace eShop.Infrastructure.AI.Plugins;

/// <summary>
/// Native C# Plugin của Semantic Kernel:
/// Cung cấp các công cụ (Tools) để mô hình AI có thể tự động gọi (Function Calling)
/// nhằm tra cứu dữ liệu sản phẩm và kiểm tra tồn kho theo thời gian thực từ Database.
/// </summary>
public class ShopCatalogPlugin
{
    private readonly IApplicationDbContext _context;
    private readonly AiChatTracker _tracker;

    public ShopCatalogPlugin(IApplicationDbContext context, AiChatTracker tracker)
    {
        _context = context;
        _tracker = tracker;
    }

    [KernelFunction, Description("Tìm kiếm danh sách sản phẩm trong kho hàng theo từ khóa tên sản phẩm, thương hiệu (hãng), ngành hàng (danh mục) hoặc mức giá tối đa (VND)")]
    public async Task<string> SearchProductsAsync(
        [Description("Từ khóa tên sản phẩm hoặc tính năng cần tìm (ví dụ: iPhone, MacBook, Chuột, MX Master)")] string? searchTerm = null,
        [Description("Mức giá tối đa mà khách hàng mong muốn (đơn vị: VNĐ)")] decimal? maxPrice = null,
        [Description("Tên hãng sản xuất / thương hiệu (ví dụ: Apple, Logitech, Samsung, Sony, Dell)")] string? brandName = null,
        [Description("Tên ngành hàng hoặc danh mục (ví dụ: Smartphones, Laptops, Phụ kiện, Âm thanh)")] string? categoryName = null)
    {
        _tracker.RecordToolInvocation(nameof(SearchProductsAsync));

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(brandName))
        {
            var brand = brandName.Trim().ToLower();
            query = query.Where(p => p.Brand != null && p.Brand.Name.ToLower().Contains(brand));
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var cat = categoryName.Trim().ToLower();
            query = query.Where(p => p.Category != null && (p.Category.Name.ToLower().Contains(cat) || (p.Category.Description != null && p.Category.Description.ToLower().Contains(cat))));
        }

        var products = await query
            .Take(5)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                PictureUrl = p.PictureUrl,
                StockQuantity = p.StockQuantity,
                BrandId = p.BrandId,
                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty
            })
            .ToListAsync();

        _tracker.AddSuggestedProducts(products);

        if (!products.Any())
        {
            return "Không tìm thấy sản phẩm nào phù hợp với tiêu chí tìm kiếm trong cơ sở dữ liệu của cửa hàng.";
        }

        var summaryList = products.Select(p => 
            $"- [ID: {p.Id}] {p.Name} | Thương hiệu: {p.BrandName} | Giá: {p.Price:N0} VND | Tồn kho: {p.StockQuantity} chiếc | Mô tả: {p.Description}");

        return $"Tìm thấy {products.Count} sản phẩm phù hợp:\n" + string.Join("\n", summaryList);
    }

    [KernelFunction, Description("Lấy thông tin chi tiết đầy đủ của một sản phẩm dựa trên ProductId")]
    public async Task<string> GetProductDetailsAsync(
        [Description("Mã định danh duy nhất (ID) của sản phẩm")] int productId)
    {
        _tracker.RecordToolInvocation(nameof(GetProductDetailsAsync));

        var p = await _context.Products
            .AsNoTracking()
            .Include(prod => prod.Brand)
            .Include(prod => prod.Category)
            .FirstOrDefaultAsync(prod => prod.Id == productId);

        if (p == null)
        {
            return $"Không tìm thấy sản phẩm có mã ID là {productId}.";
        }

        var dto = new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            PictureUrl = p.PictureUrl,
            StockQuantity = p.StockQuantity,
            BrandId = p.BrandId,
            BrandName = p.Brand?.Name ?? string.Empty,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty
        };

        _tracker.AddSuggestedProducts(new[] { dto });

        return $"Thông tin chi tiết sản phẩm:\n" +
               $"- Tên: {p.Name}\n" +
               $"- ID: {p.Id}\n" +
               $"- Thương hiệu: {p.Brand?.Name}\n" +
               $"- Ngành hàng: {p.Category?.Name}\n" +
               $"- Giá bán: {p.Price:N0} VND\n" +
               $"- Số lượng tồn kho: {p.StockQuantity} chiếc\n" +
               $"- Mô tả: {p.Description}";
    }

    [KernelFunction, Description("Kiểm tra nhanh số lượng hàng còn tồn trong kho của sản phẩm theo ProductId")]
    public async Task<string> CheckProductStockAsync(
        [Description("Mã định danh duy nhất (ID) của sản phẩm cần kiểm tra")] int productId)
    {
        _tracker.RecordToolInvocation(nameof(CheckProductStockAsync));

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return $"Sản phẩm ID {productId} không tồn tại trong hệ thống.";
        }

        if (product.StockQuantity > 0)
        {
            return $"Sản phẩm '{product.Name}' (ID: {product.Id}) hiện còn {product.StockQuantity} chiếc trong kho sẵn sàng giao.";
        }

        return $"Sản phẩm '{product.Name}' (ID: {product.Id}) hiện đang tạm thời HẾT HÀNG (Tồn kho: 0).";
    }
}
