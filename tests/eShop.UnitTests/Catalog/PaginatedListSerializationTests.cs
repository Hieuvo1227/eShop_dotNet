using System.Text.Json;
using eShop.Application.Catalog.DTOs;
using eShop.Application.Common.Models;
using Xunit;

namespace eShop.UnitTests.Catalog;

public class PaginatedListSerializationTests
{
    [Fact]
    public void PaginatedList_ShouldSerializeAndDeserialize_WithoutExceptions()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new() { Id = 1, Name = "iPhone 15 Pro Max", Price = 29990000m, StockQuantity = 10 },
            new() { Id = 2, Name = "MacBook Pro 14", Price = 49990000m, StockQuantity = 5 }
        };

        var originalList = new PaginatedList<ProductDto>(products, count: 2, pageIndex: 1, pageSize: 10);

        // Act: Serialize to JSON (giống như Web API gửi về)
        var json = JsonSerializer.Serialize(originalList);

        // Assert: Deserialize from JSON (giống như Blazor Web Client nhận được)
        var deserializedList = JsonSerializer.Deserialize<PaginatedList<ProductDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(deserializedList);
        Assert.Equal(1, deserializedList.PageIndex);
        Assert.Equal(2, deserializedList.TotalCount);
        Assert.Equal(1, deserializedList.TotalPages);
        Assert.Equal(2, deserializedList.Items.Count);
        Assert.Equal("iPhone 15 Pro Max", deserializedList.Items.First().Name);
    }
}

