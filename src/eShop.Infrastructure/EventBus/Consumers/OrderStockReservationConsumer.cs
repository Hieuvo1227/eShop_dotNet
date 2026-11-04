using eShop.Application.Common.Models.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace eShop.Infrastructure.EventBus.Consumers;

/// <summary>
/// Background Consumer: Tự động lắng nghe sự kiện OrderCreatedIntegrationEvent từ Message Broker
/// để thông báo tới phân hệ Kho vận & Đóng gói hàng hóa.
/// </summary>
public class OrderStockReservationConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderStockReservationConsumer> _logger;

    public OrderStockReservationConsumer(ILogger<OrderStockReservationConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "📦 [OrderStockReservationConsumer] Tiếp nhận Đơn hàng #{OrderId} gồm {ItemCount} mặt hàng để điều phối kho...",
            message.OrderId,
            message.Items.Count);

        foreach (var item in message.Items)
        {
            _logger.LogInformation(
                "   -> [Kho vận] Chuẩn bị sản phẩm #{ProductId} ({ProductName}) - Số lượng: {Units}",
                item.ProductId,
                item.ProductName,
                item.Units);
        }

        // Giả lập xử lý điều phối kho vận
        await Task.Delay(300, context.CancellationToken);

        _logger.LogInformation(
            "✅ [OrderStockReservationConsumer] Đã tạo phiếu xuất kho & nhãn vận chuyển (Shipping Label) cho Đơn hàng #{OrderId}!",
            message.OrderId);
    }
}

