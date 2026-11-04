using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using eShop.Application.AI.Commands.ChatWithAssistant;
using eShop.Application.AI.DTOs;
using eShop.Application.Basket.Commands.AddItemToBasket;
using eShop.Application.Basket.Commands.UpdateItemQuantity;
using eShop.Application.Basket.DTOs;
using eShop.Application.Catalog.Commands.CreateProduct;
using eShop.Application.Catalog.DTOs;
using eShop.Application.Common.Interfaces;
using eShop.Application.Common.Models;
using eShop.Application.Ordering.Commands.CreateOrder;
using eShop.Application.Ordering.DTOs;

namespace eShop.Web.Services;

public class AdminMetricsDto
{
    [JsonPropertyName("totalProducts")]
    public int TotalProducts { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("requestedBy")]
    public string RequestedBy { get; set; } = string.Empty;

    [JsonPropertyName("systemStatus")]
    public string SystemStatus { get; set; } = string.Empty;

    [JsonPropertyName("serverTimeUtc")]
    public DateTime ServerTimeUtc { get; set; }
}

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
            _cartState.SetBasket(new CustomerBasketDto { BuyerId = buyerId });
            return order;
        }

        return null;
    }

    public async Task<List<OrderDto>?> GetOrdersByBuyerIdAsync(string buyerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<OrderDto>>($"/api/orders/buyer/{buyerId}");
        }
        catch
        {
            return new List<OrderDto>();
        }
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

    #region Authentication (JWT & RBAC)
    public async Task<AuthResponseDto?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        }
        return null;
    }

    public async Task<AuthResponseDto?> RegisterAsync(string email, string password, string fullName)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/register", new { email, password, fullName });
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        }
        return null;
    }
    #endregion

    #region Admin API
    public async Task<AdminMetricsDto?> GetAdminMetricsAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/metrics");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AdminMetricsDto>();
        }
        return null;
    }

    public async Task<int?> CreateProductAsync(CreateProductCommand command, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/catalog/products")
        {
            Content = JsonContent.Create(command)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
            if (result != null && result.TryGetValue("id", out var newId))
                return newId;
        }
        return null;
    }

    public async Task<bool> UpdateProductStockAsync(int productId, int adjustment, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/catalog/products/{productId}/stock")
        {
            Content = JsonContent.Create(new { adjustment })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
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
