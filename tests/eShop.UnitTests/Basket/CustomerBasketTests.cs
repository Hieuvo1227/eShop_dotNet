using eShop.Domain.Entities.Basket;
using Xunit;

namespace eShop.UnitTests.Basket;

public class CustomerBasketTests
{
    [Fact]
    public void AddItem_ShouldAddNewItem_WhenNotExists()
    {
        // Arrange
        var basket = new CustomerBasket("user-1");

        // Act
        basket.AddItem(1, "iPhone 15", 25000000m, 1, "iphone.png");

        // Assert
        Assert.Single(basket.Items);
        Assert.Equal(1, basket.Items[0].ProductId);
        Assert.Equal(25000000m, basket.TotalAmount);
    }

    [Fact]
    public void AddItem_ShouldMergeQuantity_WhenItemAlreadyExists()
    {
        // Arrange
        var basket = new CustomerBasket("user-1");
        basket.AddItem(1, "iPhone 15", 25000000m, 1, "iphone.png");

        // Act: Thêm tiếp sản phẩm đó lần 2 với số lượng 2
        basket.AddItem(1, "iPhone 15", 25000000m, 2, "iphone.png");

        // Assert: Không thêm dòng mới mà cộng dồn số lượng = 3
        Assert.Single(basket.Items);
        Assert.Equal(3, basket.Items[0].Quantity);
        Assert.Equal(75000000m, basket.TotalAmount);
    }

    [Fact]
    public void UpdateQuantity_ShouldRemoveItem_WhenQuantityIsZeroOrNegative()
    {
        // Arrange
        var basket = new CustomerBasket("user-1");
        basket.AddItem(1, "iPhone 15", 25000000m, 2, "iphone.png");

        // Act: Đổi số lượng về 0
        basket.UpdateQuantity(1, 0);

        // Assert: Món hàng tự động bị loại khỏi giỏ
        Assert.Empty(basket.Items);
        Assert.Equal(0m, basket.TotalAmount);
    }

    [Fact]
    public void TotalAmount_ShouldCalculateSumOfAllItemsCorrectly()
    {
        // Arrange
        var basket = new CustomerBasket("user-1");
        basket.AddItem(1, "Chuột Logitech", 2000000m, 2, "mouse.png"); // 4.000.000
        basket.AddItem(2, "Bàn phím cơ", 3000000m, 1, "keyboard.png"); // 3.000.000

        // Assert
        Assert.Equal(7000000m, basket.TotalAmount);
    }
}

