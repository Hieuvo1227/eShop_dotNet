using System.Collections.Concurrent;
using eShop.Application.Common.Interfaces;
using eShop.Domain.Entities.Basket;
using Microsoft.Extensions.Logging;

namespace eShop.Infrastructure.Repositories;

/// <summary>
/// Kho lưu trữ Giỏ hàng In-Memory dự phòng:
/// Hoạt động tương tự Redis trong bộ nhớ RAM của ứng dụng khi môi trường phát triển chưa bật Redis Server.
/// </summary>
public class InMemoryBasketRepository : IBasketRepository
{
    private readonly ConcurrentDictionary<string, CustomerBasket> _baskets = new();
    private readonly ILogger<InMemoryBasketRepository> _logger;

    public InMemoryBasketRepository(ILogger<InMemoryBasketRepository> logger)
    {
        _logger = logger;
    }

    public Task<CustomerBasket?> GetBasketAsync(string buyerId)
    {
        _baskets.TryGetValue(buyerId, out var basket);
        return Task.FromResult(basket);
    }

    public Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        _baskets[basket.BuyerId] = basket;
        _logger.LogInformation("InMemoryBasket: Đã lưu giỏ hàng cho {BuyerId}", basket.BuyerId);
        return Task.FromResult(basket);
    }

    public Task<bool> DeleteBasketAsync(string buyerId)
    {
        var removed = _baskets.TryRemove(buyerId, out _);
        return Task.FromResult(removed);
    }
}

