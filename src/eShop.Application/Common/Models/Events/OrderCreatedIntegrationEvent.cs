namespace eShop.Application.Common.Models.Events;

/// <summary>
/// Integration Event (Thông điệp Tích hợp) bắn ra Message Broker khi đơn hàng được tạo thành công.
/// Khác với Domain Event nội bộ, Integration Event được thiết kế để truyền tải dữ liệu
/// giữa các phân hệ (hoặc các Microservices độc lập) qua mạng.
/// </summary>
public record OrderCreatedIntegrationEvent
{
    public int OrderId { get; init; }
    public string BuyerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public List<OrderItemIntegrationDto> Items { get; init; } = new();
}

public record OrderItemIntegrationDto(
    int ProductId, 
    string ProductName, 
    decimal UnitPrice, 
    int Units);

