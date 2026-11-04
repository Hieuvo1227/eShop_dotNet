using System.Text.Json;
using eShop.Application.Common.Interfaces;
using eShop.Domain.Entities.Basket;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace eShop.Infrastructure.Repositories;

/// <summary>
/// Kho lưu trữ Giỏ hàng sử dụng Redis (Chuẩn eShop của Microsoft).
/// Đọc/Ghi dữ liệu JSON tốc độ micro-giây và tự động hết hạn sau 30 ngày (TTL).
/// </summary>
public class RedisBasketRepository : IBasketRepository
{
    private readonly ILogger<RedisBasketRepository> _logger;
    private readonly IDatabase _database;
    private static readonly TimeSpan BasketTimeToLive = TimeSpan.FromDays(30);

    public RedisBasketRepository(ILogger<RedisBasketRepository> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _database = redis.GetDatabase();
    }

    private static RedisKey GetBasketKey(string buyerId) => $"basket:{buyerId}";

    public async Task<CustomerBasket?> GetBasketAsync(string buyerId)
    {
        var data = await _database.StringGetAsync(GetBasketKey(buyerId));

        if (data.IsNullOrEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CustomerBasket>(data!, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi giải mã giỏ hàng từ Redis cho BuyerId: {BuyerId}", buyerId);
            return null;
        }
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        var key = GetBasketKey(basket.BuyerId);
        var json = JsonSerializer.Serialize(basket, JsonOptions);

        // Lưu vào Redis và thiết lập thời gian sống (TTL) 30 ngày
        var created = await _database.StringSetAsync(key, json, BasketTimeToLive);

        if (!created)
        {
            _logger.LogError("Không thể lưu giỏ hàng vào Redis cho BuyerId: {BuyerId}", basket.BuyerId);
            throw new InvalidOperationException($"Lỗi lưu giỏ hàng cho khách hàng {basket.BuyerId} vào Redis.");
        }

        _logger.LogInformation("Đã cập nhật giỏ hàng trên Redis cho BuyerId: {BuyerId}", basket.BuyerId);
        return (await GetBasketAsync(basket.BuyerId))!;
    }

    public async Task<bool> DeleteBasketAsync(string buyerId)
    {
        return await _database.KeyDeleteAsync(GetBasketKey(buyerId));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

