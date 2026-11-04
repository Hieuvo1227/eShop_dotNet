using eShop.Domain.Entities.Ordering;
using eShop.Domain.Enums;
using eShop.Domain.Events;
using eShop.Domain.ValueObjects;
using Xunit;

namespace eShop.UnitTests.Ordering;

public class OrderAggregateTests
{
    [Fact]
    public void Address_ValueObjectEquality_ShouldBeEqualWhenAllPropertiesMatch()
    {
        // Arrange
        var address1 = new Address("123 Le Loi", "Quan 1", "TP. Ho Chi Minh", "70000");
        var address2 = new Address("123 Le Loi", "Quan 1", "TP. Ho Chi Minh", "70000");

        // Act & Assert (Đặc tính Value Object trong DDD: So sánh bằng giá trị, không so sánh địa chỉ ô nhớ)
        Assert.Equal(address1, address2);
        Assert.True(address1 == address2);
    }

    [Fact]
    public void Order_Creation_ShouldStartWithSubmittedStatus()
    {
        // Arrange
        var address = new Address("123 Le Loi", "Quan 1", "TP. Ho Chi Minh");

        // Act
        var order = new Order("buyer-1", address);

        // Assert
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal("buyer-1", order.BuyerId);
        Assert.Empty(order.OrderItems);
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void Order_AddOrderItem_ShouldCalculateTotalAmountCorrectly()
    {
        // Arrange
        var order = new Order("buyer-1", new Address("123 Le Loi", "Quan 1", "TP. Ho Chi Minh"));

        // Act
        order.AddOrderItem(1, "iPhone 15", 25000000m, 2, "iphone.png"); // 50.000.000
        order.AddOrderItem(2, "Chuột Logitech", 2000000m, 1, "mouse.png"); // 2.000.000

        // Assert
        Assert.Equal(2, order.OrderItems.Count);
        Assert.Equal(52000000m, order.TotalAmount);
    }

    [Fact]
    public void Order_SetPaidStatus_ShouldEmitOrderPaidDomainEvent()
    {
        // Arrange
        var order = new Order("buyer-1", new Address("123 Le Loi", "Quan 1", "TP. Ho Chi Minh"));
        order.AddOrderItem(1, "iPhone 15", 25000000m, 1, "iphone.png");

        // Act
        order.SetPaidStatus();

        // Assert
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Contains(order.DomainEvents, e => e is OrderPaidDomainEvent);
    }

    [Fact]
    public void Order_SetPaidStatus_ShouldThrowException_WhenOrderIsCancelled()
    {
        // Arrange
        var order = new Order("buyer-1", new Address("123 Le Loi", "Quan 1", "TP. Ho Chi Minh"));
        order.SetCancelledStatus();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.SetPaidStatus());
    }

    [Fact]
    public void Order_SetShippedStatus_ShouldThrowException_WhenOrderIsNotPaid()
    {
        // Arrange
        var order = new Order("buyer-1", new Address("123 Le Loi", "Quan 1", "TP. Ho Chi Minh"));

        // Act & Assert (Chưa thanh toán không được phép giao hàng)
        Assert.Throws<InvalidOperationException>(() => order.SetShippedStatus());
    }
}

