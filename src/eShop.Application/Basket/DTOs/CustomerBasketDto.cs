namespace eShop.Application.Basket.DTOs;

public class CustomerBasketDto
{
    public string BuyerId { get; set; } = string.Empty;
    public List<BasketItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int TotalItems => Items.Sum(i => i.Quantity);
}

