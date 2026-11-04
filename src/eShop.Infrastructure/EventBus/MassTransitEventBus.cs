using eShop.Application.Common.Interfaces;
using MassTransit;

namespace eShop.Infrastructure.EventBus;

/// <summary>
/// Triển khai IEventBus bằng thư viện MassTransit.
/// Tận dụng IPublishEndpoint để phát tán thông điệp (Publish/Subscribe pattern).
/// </summary>
public class MassTransitEventBus : IEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        await _publishEndpoint.Publish(message, cancellationToken);
    }
}

