namespace eShop.Application.Basket.DTOs;

public class BasketItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? PictureUrl { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
}

