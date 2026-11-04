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
    public string PaymentMethod { get; private set; } = "COD";
    public string? PaymentTransactionId { get; private set; }

    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public decimal TotalAmount => _orderItems.Sum(i => i.TotalPrice);

    // Constructor phục vụ EF Core
    private Order() { }

    public Order(string buyerId, Address shippingAddress, string paymentMethod = "COD")
    {
        if (string.IsNullOrWhiteSpace(buyerId))
        {
            throw new ArgumentException("BuyerId không được để trống.", nameof(buyerId));
        }

        BuyerId = buyerId;
        ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress));
        PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "COD" : paymentMethod.Trim();
        Status = PaymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase) 
            ? OrderStatus.AwaitingPayment 
            : OrderStatus.Submitted;
    }

    /// <summary>
    /// Thiết lập phương thức thanh toán cho đơn hàng.
    /// </summary>
    public void SetPaymentMethod(string paymentMethod)
    {
        PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "COD" : paymentMethod.Trim();
    }

    /// <summary>
    /// Chuyển trạng thái: Đang chờ thanh toán cổng trực tuyến (AwaitingPayment).
    /// </summary>
    public void SetAwaitingPaymentStatus()
    {
        if (Status == OrderStatus.Paid || Status == OrderStatus.Cancelled) return;
        Status = OrderStatus.AwaitingPayment;
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
    public void SetPaidStatus(string? transactionId = null)
    {
        if (Status == OrderStatus.Paid) return;

        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Không thể thanh toán một đơn hàng đã bị hủy.");
        }

        Status = OrderStatus.Paid;
        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            PaymentTransactionId = transactionId.Trim();
        }

        AddDomainEvent(new OrderPaidDomainEvent(Id));
    }

    /// <summary>
    /// Chuyển trạng thái: Đang giao hàng (Shipped).
    /// Bắt buộc đơn hàng phải ở trạng thái Đã thanh toán (Paid) theo nghiệp vụ eShop.
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
    /// Chuyển trạng thái: Đã hoàn tất (Completed).
    /// </summary>
    public void SetCompletedStatus()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Không thể hoàn tất một đơn hàng đã bị hủy.");
        }

        Status = OrderStatus.Completed;
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

