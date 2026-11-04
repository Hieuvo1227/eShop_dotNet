namespace eShop.Domain.Enums;

/// <summary>
/// Máy trạng thái (State Machine) của một đơn hàng.
/// </summary>
public enum OrderStatus
{
    Submitted = 1,       // Đã đặt hàng, chờ xử lý
    AwaitingPayment = 2, // Đang chờ cổng thanh toán xác nhận
    Paid = 3,            // Đã thanh toán thành công
    Shipped = 4,         // Đang giao hàng
    Completed = 5,       // Đã hoàn tất
    Cancelled = 6        // Đã hủy
}

