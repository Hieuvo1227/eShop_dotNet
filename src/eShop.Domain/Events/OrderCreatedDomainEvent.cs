using eShop.Domain.Common;

namespace eShop.Domain.Events;

/// <summary>
/// Sự kiện miền phát sinh khi một đơn hàng mới được tạo thành công.
/// </summary>
public record OrderCreatedDomainEvent(int OrderId, string BuyerId, decimal TotalAmount) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

