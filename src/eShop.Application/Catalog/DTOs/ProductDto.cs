namespace eShop.Application.Catalog.DTOs;

/// <summary>
/// DTO (Data Transfer Object) trả về dữ liệu sản phẩm cho phía Client / UI.
/// Chỉ chứa thông tin cần thiết, che giấu các trường nội bộ của Entity.
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int AvailableStock => StockQuantity;
    public string? PictureUrl { get; set; }

    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

