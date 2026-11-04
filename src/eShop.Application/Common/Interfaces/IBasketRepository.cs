using eShop.Domain.Entities.Basket;

namespace eShop.Application.Common.Interfaces;

/// <summary>
/// Giao diện trừu tượng hóa kho lưu trữ Giỏ hàng (Dependency Inversion).
/// Cho phép Application gọi lưu/đọc giỏ hàng mà không phụ thuộc trực tiếp vào Redis SDK.
/// </summary>
public interface IBasketRepository
{
    Task<CustomerBasket?> GetBasketAsync(string buyerId);
    Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string buyerId);
}

