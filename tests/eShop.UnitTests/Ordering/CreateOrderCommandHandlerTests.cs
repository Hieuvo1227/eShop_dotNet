using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using eShop.Application.Common.Models.Events;
using eShop.Application.Ordering.Commands.CreateOrder;
using eShop.Application.Ordering.DTOs;
using eShop.Domain.Entities;
using eShop.Domain.Entities.Basket;
using eShop.Domain.Entities.Ordering;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ValidationException = eShop.Application.Common.Exceptions.ValidationException;

namespace eShop.UnitTests.Ordering;

public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().OwnsOne(o => o.ShippingAddress);
        base.OnModelCreating(modelBuilder);
    }
}

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IBasketRepository> _basketRepositoryMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();

    private TestDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenBasketIsEmpty()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        _basketRepositoryMock
            .Setup(r => r.GetBasketAsync("buyer-empty"))
            .ReturnsAsync(new CustomerBasket("buyer-empty"));

        var handler = new CreateOrderCommandHandler(context, _basketRepositoryMock.Object, _eventBusMock.Object);
        var command = new CreateOrderCommand
        {
            BuyerId = "buyer-empty",
            ShippingAddress = new AddressDto { Street = "123 Le Loi", District = "Quan 1", City = "HCM" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var basket = new CustomerBasket("buyer-1");
        basket.AddItem(999, "Sản phẩm ảo", 100000m, 1, "virtual.png"); // Sản phẩm 999 không có trong DB

        _basketRepositoryMock
            .Setup(r => r.GetBasketAsync("buyer-1"))
            .ReturnsAsync(basket);

        var handler = new CreateOrderCommandHandler(context, _basketRepositoryMock.Object, _eventBusMock.Object);
        var command = new CreateOrderCommand
        {
            BuyerId = "buyer-1",
            ShippingAddress = new AddressDto { Street = "123 Le Loi", District = "Quan 1", City = "HCM" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldDeductStock_CreateOrder_DeleteBasket_AndPublishIntegrationEvent()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var brand = new Brand { Id = 1, Name = "Apple" };
        var category = new Category { Id = 1, Name = "Smartphones" };
        var product = new Product
        {
            Id = 1,
            Name = "iPhone 15 Pro Max",
            Description = "Flagship Apple",
            Price = 29990000m,
            StockQuantity = 10,
            BrandId = 1,
            CategoryId = 1,
            PictureUrl = "iphone.png"
        };
        context.Brands.Add(brand);
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var basket = new CustomerBasket("buyer-vip");
        basket.AddItem(1, "iPhone 15 Pro Max", 29990000m, 2, "iphone.png"); // Mua 2 chiếc

        _basketRepositoryMock
            .Setup(r => r.GetBasketAsync("buyer-vip"))
            .ReturnsAsync(basket);

        var handler = new CreateOrderCommandHandler(context, _basketRepositoryMock.Object, _eventBusMock.Object);
        var command = new CreateOrderCommand
        {
            BuyerId = "buyer-vip",
            ShippingAddress = new AddressDto
            {
                Street = "72 Le Thanh Ton",
                District = "Quan 1",
                City = "TP. Ho Chi Minh",
                ZipCode = "70000"
            }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert Order
        Assert.NotNull(result);
        Assert.Equal("buyer-vip", result.BuyerId);
        Assert.Equal(59980000m, result.TotalAmount);
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Units);

        // Assert Stock Deduction (10 - 2 = 8)
        var updatedProduct = await context.Products.FindAsync(1);
        Assert.NotNull(updatedProduct);
        Assert.Equal(8, updatedProduct.StockQuantity);

        // Assert Basket Deletion from Redis
        _basketRepositoryMock.Verify(r => r.DeleteBasketAsync("buyer-vip"), Times.Once);

        // Assert Event Bus Publishing (Integration Event)
        _eventBusMock.Verify(bus => bus.PublishAsync(
            It.Is<OrderCreatedIntegrationEvent>(e => 
                e.OrderId == result.Id && 
                e.BuyerId == "buyer-vip" && 
                e.TotalAmount == 59980000m &&
                e.Items.Count == 1 &&
                e.Items[0].Units == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
