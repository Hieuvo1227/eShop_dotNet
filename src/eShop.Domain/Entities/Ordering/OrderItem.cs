using eShop.Domain.Common;

namespace eShop.Domain.Entities.Ordering;

/// <summary>
/// Thực thể Dòng đơn hàng (Child Entity thuộc Aggregate Root Order).
/// Lưu lại giá bán chốt tại thời điểm mua, hoàn toàn độc lập với giá sau này của Catalog.
/// </summary>
public class OrderItem : BaseEntity
{
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Units { get; private set; }
    public string? PictureUrl { get; private set; }

    public int OrderId { get; internal set; }

    public decimal TotalPrice => UnitPrice * Units;

    // Constructor phục vụ EF Core
    private OrderItem() { }

    public OrderItem(int productId, string productName, decimal unitPrice, int units, string? pictureUrl)
    {
        if (units <= 0)
        {
            throw new ArgumentException("Số lượng đặt hàng phải lớn hơn 0.", nameof(units));
        }

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Units = units;
        PictureUrl = pictureUrl;
    }

    public void AddUnits(int units)
    {
        if (units <= 0) return;
        Units += units;
    }
}

