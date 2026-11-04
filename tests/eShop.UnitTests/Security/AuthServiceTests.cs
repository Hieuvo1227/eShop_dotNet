using eShop.Application.Common.Exceptions;
using eShop.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace eShop.UnitTests.Security;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "eShop_Test_Super_Secret_Key_For_UnitTesting_Only_32bytes_long!",
                ["JwtSettings:Issuer"] = "eShop.API",
                ["JwtSettings:Audience"] = "eShop.Client",
                ["JwtSettings:ExpiryMinutes"] = "60"
            })
            .Build();

        _authService = new AuthService(configuration);
    }

    [Fact]
    public async Task LoginAsync_WithValidAdminCredentials_ReturnsAdminToken()
    {
        // Act
        var result = await _authService.LoginAsync("admin@eshop.com", "Admin@123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin@eshop.com", result.Email);
        Assert.Equal("Admin", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiresInSeconds > 0);
    }

    [Fact]
    public async Task LoginAsync_WithValidCustomerCredentials_ReturnsCustomerToken()
    {
        // Act
        var result = await _authService.LoginAsync("user@eshop.com", "User@123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user@eshop.com", result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        // Act
        var result = await _authService.LoginAsync("admin@eshop.com", "WrongPassword!");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _authService.LoginAsync("nonexistent@eshop.com", "Admin@123");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsCustomerToken()
    {
        // Arrange
        var newEmail = $"user_{Guid.NewGuid():N}@eshop.com";

        // Act
        var result = await _authService.RegisterAsync(newEmail, "Password123!", "Khách Hàng Mới");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newEmail, result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.Equal("Khách Hàng Mới", result.FullName);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _authService.RegisterAsync("admin@eshop.com", "Password123!", "Trùng Lặp"));
    }
}

