namespace eShop.Application.Common.Interfaces;

/// <summary>
/// Giao diện trừu tượng hóa Message Bus (Nguyên lý Đảo ngược Phụ thuộc DIP).
/// Tầng Application chỉ cần gọi PublishAsync mà không cần quan tâm
/// công nghệ bên dưới là RabbitMQ, Kafka hay Azure Service Bus.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

