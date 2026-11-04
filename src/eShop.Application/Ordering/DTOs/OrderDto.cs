namespace eShop.Application.Ordering.DTOs;

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string Country { get; set; } = "Vietnam";
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
    public string? PictureUrl { get; set; }
    public decimal TotalPrice => UnitPrice * Units;
}

public class OrderDto
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public DateTime OrderDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public AddressDto ShippingAddress { get; set; } = new();
    public List<OrderItemDto> Items { get; set; } = new();
}

