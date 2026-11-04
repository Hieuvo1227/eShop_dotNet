namespace eShop.Domain.Entities.Basket;

/// <summary>
/// Món hàng trong giỏ (Snapshot của sản phẩm tại thời điểm đưa vào giỏ).
/// </summary>
public class BasketItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? PictureUrl { get; set; }

    public decimal TotalPrice => UnitPrice * Quantity;
}

