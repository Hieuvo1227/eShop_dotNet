using eShop.Domain.Common;

namespace eShop.Domain.Events;

/// <summary>
/// Sự kiện miền phát sinh khi một sản phẩm bị hết hàng trong kho (StockQuantity = 0).
/// </summary>
public record ProductOutOfStockDomainEvent(int ProductId, string SKU) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

