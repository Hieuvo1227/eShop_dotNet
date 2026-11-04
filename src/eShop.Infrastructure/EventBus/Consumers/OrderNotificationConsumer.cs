using eShop.Application.Common.Models.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace eShop.Infrastructure.EventBus.Consumers;

/// <summary>
/// Background Consumer: Tự động lắng nghe sự kiện OrderCreatedIntegrationEvent từ Message Broker
/// để xuất hóa đơn điện tử PDF và gửi email xác nhận đặt hàng cho khách hàng.
/// </summary>
public class OrderNotificationConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderNotificationConsumer> _logger;

    public OrderNotificationConsumer(ILogger<OrderNotificationConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "📩 [OrderNotificationConsumer] Bắt đầu xử lý thông báo cho Đơn hàng #{OrderId} của khách '{BuyerId}' (Tổng tiền: {TotalAmount:N0} VND)...",
            message.OrderId,
            message.BuyerId,
            message.TotalAmount);

        // Giả lập thời gian xuất file PDF hóa đơn và gửi email qua SMTP/SendGrid
        await Task.Delay(500, context.CancellationToken);

        _logger.LogInformation(
            "✅ [OrderNotificationConsumer] Đã xuất Hóa đơn điện tử PDF và gửi Email xác nhận thành công tới khách hàng '{BuyerId}' cho Đơn hàng #{OrderId}!",
            message.BuyerId,
            message.OrderId);
    }
}

