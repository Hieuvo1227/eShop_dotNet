using eShop.Domain.Common;

namespace eShop.Domain.Events;

/// <summary>
/// Sự kiện miền phát sinh khi một đơn hàng được thanh toán thành công.
/// </summary>
public record OrderPaidDomainEvent(int OrderId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

