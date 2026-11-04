using eShop.Domain.Common;
using eShop.Domain.Events;

namespace eShop.Domain.Entities;

/// <summary>
/// Thực thể Sản phẩm cốt lõi trong hệ thống Catalog.
/// Quản lý thông tin định danh, giá, tồn kho và các quy tắc nghiệp vụ liên quan.
/// </summary>
public class Product : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? PictureUrl { get; set; }

    // Khóa ngoại liên kết Brand
    public int BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    // Khóa ngoại liên kết Category
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Phương thức nghiệp vụ: Cập nhật số lượng tồn kho (Rich Domain Model).
    /// </summary>
    /// <param name="adjustment">Số lượng thay đổi (+ thêm hàng, - xuất hàng)</param>
    public void UpdateStock(int adjustment)
    {
        var newQuantity = StockQuantity + adjustment;

        if (newQuantity < 0)
        {
            throw new InvalidOperationException(
                $"Không đủ số lượng hàng tồn kho cho sản phẩm '{Name}' (SKU: {SKU}). Hiện còn: {StockQuantity}, yêu cầu giảm: {Math.Abs(adjustment)}.");
        }

        StockQuantity = newQuantity;

        // Nếu số lượng về 0 -> Bắn sự kiện miền hết hàng
        if (StockQuantity == 0)
        {
            AddDomainEvent(new ProductOutOfStockDomainEvent(Id, SKU));
        }
    }
}

