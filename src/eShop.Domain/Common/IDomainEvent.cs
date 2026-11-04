namespace eShop.Domain.Common;

/// <summary>
/// Đánh dấu một sự kiện nghiệp vụ phát sinh bên trong Domain (Domain Event).
/// Được phát đi khi trạng thái của Entity thay đổi (ví dụ: OrderCreated, StockDecreased).
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}

