using eShop.Application.Basket.DTOs;

namespace eShop.Web.Services;

/// <summary>
/// Quản lý trạng thái Giỏ hàng tập trung phía Client (Blazor State Management).
/// Kích hoạt sự kiện OnChange để cập nhật số lượng món trên Navbar khi có thay đổi.
/// </summary>
public class CartStateService
{
    public string BuyerId { get; set; } = "buyer-demo";
    public CustomerBasketDto? Basket { get; private set; }

    public int TotalItemCount => Basket?.Items.Sum(i => i.Quantity) ?? 0;
    public decimal TotalAmount => Basket?.TotalAmount ?? 0m;

    public event Action? OnChange;

    public void SetBasket(CustomerBasketDto basket)
    {
        Basket = basket;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChange?.Invoke();
}

