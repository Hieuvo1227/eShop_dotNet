using eShop.Application.AI.Commands.ChatWithAssistant;
using eShop.Application.AI.DTOs;
using eShop.Application.Basket.Commands.AddItemToBasket;
using eShop.Application.Basket.Commands.UpdateItemQuantity;
using eShop.Application.Basket.DTOs;
using eShop.Application.Catalog.DTOs;
using eShop.Application.Common.Models;
using eShop.Application.Ordering.Commands.CreateOrder;
using eShop.Application.Ordering.DTOs;

namespace eShop.Web.Services;

/// <summary>
/// Typed HttpClient giao tiếp với toàn bộ REST Endpoints của eShop.API.
/// </summary>
public class ShopApiClient
{
    private readonly HttpClient _http;
    private readonly CartStateService _cartState;

    public ShopApiClient(HttpClient http, CartStateService cartState)
    {
        _http = http;
        _cartState = cartState;
    }

    #region Catalog
    public async Task<PaginatedList<ProductDto>?> GetProductsAsync(
        int pageIndex = 1, 
        int pageSize = 12, 
        int? brandId = null, 
        int? categoryId = null, 
        string? searchTerm = null)
    {
        var url = $"/api/catalog/products?pageIndex={pageIndex}&pageSize={pageSize}";
        if (brandId.HasValue) url += $"&brandId={brandId.Value}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        if (!string.IsNullOrWhiteSpace(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";

        return await _http.GetFromJsonAsync<PaginatedList<ProductDto>>(url);
    }

    public async Task<List<BrandDto>?> GetBrandsAsync()
    {
        return await _http.GetFromJsonAsync<List<BrandDto>>("/api/catalog/brands");
    }

    public async Task<List<CategoryDto>?> GetCategoriesAsync()
    {
        return await _http.GetFromJsonAsync<List<CategoryDto>>("/api/catalog/categories");
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<ProductDto>($"/api/catalog/products/{id}");
    }
    #endregion

    #region Basket
    public async Task<CustomerBasketDto?> GetBasketAsync(string buyerId)
    {
        try
        {
            var basket = await _http.GetFromJsonAsync<CustomerBasketDto>($"/api/basket/{buyerId}");
            if (basket != null) _cartState.SetBasket(basket);
            return basket;
        }
        catch
        {
            return new CustomerBasketDto { BuyerId = buyerId };
        }
    }

    public async Task<CustomerBasketDto?> AddItemToBasketAsync(string buyerId, int productId, int quantity = 1)
    {
        var response = await _http.PostAsJsonAsync($"/api/basket/{buyerId}/items", new AddItemToBasketCommand
        {
            BuyerId = buyerId,
            ProductId = productId,
            Quantity = quantity
        });

        if (response.IsSuccessStatusCode)
        {
            var basket = await response.Content.ReadFromJsonAsync<CustomerBasketDto>();
            if (basket != null) _cartState.SetBasket(basket);
            return basket;
        }

        return null;
    }

    public async Task<CustomerBasketDto?> UpdateQuantityAsync(string buyerId, int productId, int quantity)
    {
        var response = await _http.PutAsJsonAsync(
            $"/api/basket/{buyerId}/items/{productId}", 
            new UpdateItemQuantityCommand(buyerId, productId, quantity));

        if (response.IsSuccessStatusCode)
        {
            var basket = await response.Content.ReadFromJsonAsync<CustomerBasketDto>();
            if (basket != null) _cartState.SetBasket(basket);
            return basket;
        }

        return null;
    }

    public async Task<CustomerBasketDto?> RemoveItemAsync(string buyerId, int productId)
    {
        var response = await _http.DeleteAsync($"/api/basket/{buyerId}/items/{productId}");
        if (response.IsSuccessStatusCode)
        {
            var basket = await response.Content.ReadFromJsonAsync<CustomerBasketDto>();
            if (basket != null) _cartState.SetBasket(basket);
            return basket;
        }

        return null;
    }

    public async Task ClearBasketAsync(string buyerId)
    {
        await _http.DeleteAsync($"/api/basket/{buyerId}");
        _cartState.SetBasket(new CustomerBasketDto { BuyerId = buyerId });
    }
    #endregion

    #region Ordering
    public async Task<OrderDto?> CreateOrderAsync(string buyerId, AddressDto shippingAddress)
    {
        var response = await _http.PostAsJsonAsync("/api/orders", new CreateOrderCommand
        {
            BuyerId = buyerId,
            ShippingAddress = shippingAddress
        });

        if (response.IsSuccessStatusCode)
        {
            var order = await response.Content.ReadFromJsonAsync<OrderDto>();
            // Sau khi tạo đơn thành công, giỏ hàng tự động xóa
            _cartState.SetBasket(new CustomerBasketDto { BuyerId = buyerId });
            return order;
        }

        return null;
    }

    public async Task<List<OrderDto>?> GetOrdersByBuyerIdAsync(string buyerId)
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>($"/api/orders/buyer/{buyerId}");
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<OrderDto>($"/api/orders/{id}");
    }

    public async Task<bool> CancelOrderAsync(int id)
    {
        var response = await _http.PutAsync($"/api/orders/{id}/cancel", null);
        return response.IsSuccessStatusCode;
    }
    #endregion

    #region AI Assistant
    public async Task<ChatAssistantResponseDto?> ChatWithAiAsync(string message, List<ChatMessageDto>? history = null)
    {
        var response = await _http.PostAsJsonAsync("/api/ai/chat", new ChatWithAssistantCommand
        {
            Message = message,
            History = history
        });

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ChatAssistantResponseDto>();
        }

        return new ChatAssistantResponseDto
        {
            Message = "Rất tiếc, hiện tại không thể kết nối tới dịch vụ AI Assistant. Vui lòng thử lại sau ít phút!"
        };
    }
    #endregion
}
