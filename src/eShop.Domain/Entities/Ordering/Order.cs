using eShop.Domain.Common;
using eShop.Domain.Enums;
using eShop.Domain.Events;
using eShop.Domain.ValueObjects;

namespace eShop.Domain.Entities.Ordering;

/// <summary>
/// Aggregate Root: Đơn hàng (Trọng tâm của phân hệ Ordering trong DDD).
/// Chịu trách nhiệm bảo vệ toàn vẹn dữ liệu cho toàn bộ các OrderItem bên trong.
/// </summary>
public class Order : BaseAuditableEntity
{
    public string BuyerId { get; private set; } = string.Empty;
    public Address ShippingAddress { get; private set; } = null!;
    public OrderStatus Status { get; private set; } = OrderStatus.Submitted;

    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public decimal TotalAmount => _orderItems.Sum(i => i.TotalPrice);

    // Constructor phục vụ EF Core
    private Order() { }

    public Order(string buyerId, Address shippingAddress)
    {
        if (string.IsNullOrWhiteSpace(buyerId))
        {
            throw new ArgumentException("BuyerId không được để trống.", nameof(buyerId));
        }

        BuyerId = buyerId;
        ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
        Status = OrderStatus.Submitted;
    }

    /// <summary>
    /// Thêm một dòng sản phẩm vào đơn hàng (Aggregate quản lý OrderItems).
    /// </summary>
    public void AddOrderItem(int productId, string productName, decimal unitPrice, int units, string? pictureUrl)
    {
        var existingItem = _orderItems.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.AddUnits(units);
        }
        else
        {
            _orderItems.Add(new OrderItem(productId, productName, unitPrice, units, pictureUrl));
        }
    }

    /// <summary>
    /// Chuyển trạng thái: Đã thanh toán (Paid).
    /// </summary>
    public void SetPaidStatus()
    {
        if (Status == OrderStatus.Paid) return;

        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Không thể thanh toán một đơn hàng đã bị hủy.");
        }

        Status = OrderStatus.Paid;
        AddDomainEvent(new OrderPaidDomainEvent(Id));
    }

    /// <summary>
    /// Chuyển trạng thái: Đang giao hàng (Shipped).
    /// </summary>
    public void SetShippedStatus()
    {
        if (Status != OrderStatus.Paid)
        {
            throw new InvalidOperationException("Chỉ đơn hàng đã thanh toán mới có thể chuyển sang giao hàng.");
        }

        Status = OrderStatus.Shipped;
    }

    /// <summary>
    /// Chuyển trạng thái: Đã hủy (Cancelled).
    /// </summary>
    public void SetCancelledStatus()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException("Không thể hủy đơn hàng đã hoặc đang được giao.");
        }

        Status = OrderStatus.Cancelled;
    }
}

