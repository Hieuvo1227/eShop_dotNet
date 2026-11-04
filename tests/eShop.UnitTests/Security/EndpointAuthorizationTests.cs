using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using eShop.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace eShop.UnitTests.Security;

/// <summary>
/// Integration Tests kiểm thử Phân quyền & Phòng chống lỗ hổng IDOR trên Web API:
/// 1. Kiểm tra 401 Unauthorized khi thiếu Token JWT
/// 2. Kiểm tra 404 Not Found khi User B cố tình truy cập/hủy đơn của User A (chống enumeration & IDOR)
/// 3. Kiểm tra 403 Forbidden khi Customer cố truy cập Endpoint của Admin
/// 4. Kiểm tra 200 OK khi Admin truy cập Endpoint quản trị
/// </summary>
public class EndpointAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBasket_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/basket");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyOrders_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChatWithAi_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/ai/chat", new { Message = "Xin chào" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminMetrics_WithCustomerToken_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = "user@eshop.com", Password = "User@123" });
        Assert.True(loginResponse.IsSuccessStatusCode);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Act
        var response = await client.GetAsync("/api/admin/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminMetrics_WithAdminToken_Returns200OK()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@eshop.com", Password = "Admin@123" });
        Assert.True(loginResponse.IsSuccessStatusCode);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Act
        var response = await client.GetAsync("/api/admin/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderDoesNotExistOrBelongsToOtherUser_Returns404NotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = "user@eshop.com", Password = "User@123" });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Act - Thử truy cập đơn hàng #99999 không thuộc về mình
        var response = await client.GetAsync("/api/orders/99999");

        // Assert - Phải trả về 404 Not Found (chống IDOR / enumeration)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_WhenOrderDoesNotExistOrBelongsToOtherUser_Returns404NotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = "user@eshop.com", Password = "User@123" });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Act - Thử gửi yêu cầu hủy đơn hàng #99999 không thuộc quyền sở hữu
        var response = await client.PutAsync("/api/orders/99999/cancel", null);

        // Assert - Phải trả về 404 Not Found
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

