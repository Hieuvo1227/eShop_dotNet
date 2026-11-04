using eShop.Application.Common.Interfaces;
using eShop.Domain.Entities;
using eShop.Domain.Entities.Ordering;
using eShop.Infrastructure.AI;
using eShop.Infrastructure.AI.Plugins;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eShop.UnitTests.AI;

public class AiTestDbContext : DbContext, IApplicationDbContext
{
    public AiTestDbContext(DbContextOptions<AiTestDbContext> options) : base(options) { }

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

public class ShopCatalogPluginTests
{
    private AiTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AiTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AiTestDbContext(options);
        context.Database.EnsureCreated();

        var apple = new Brand { Id = 1, Name = "Apple" };
        var logitech = new Brand { Id = 2, Name = "Logitech" };
        var phones = new Category { Id = 1, Name = "Smartphones" };
        var accessories = new Category { Id = 2, Name = "Accessories" };

        context.Brands.AddRange(apple, logitech);
        context.Categories.AddRange(phones, accessories);

        context.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "iPhone 15 Pro Max",
                Description = "Flagship Apple Titanium",
                Price = 29990000m,
                StockQuantity = 8,
                BrandId = 1,
                CategoryId = 1,
                PictureUrl = "iphone.png"
            },
            new Product
            {
                Id = 2,
                Name = "MacBook Pro 14 M3",
                Description = "Laptop chuyên nghiệp Apple",
                Price = 45990000m,
                StockQuantity = 5,
                BrandId = 1,
                CategoryId = 1,
                PictureUrl = "macbook.png"
            },
            new Product
            {
                Id = 3,
                Name = "Chuột Logitech MX Master 3S",
                Description = "Chuột công thái học cao cấp",
                Price = 2490000m,
                StockQuantity = 15,
                BrandId = 2,
                CategoryId = 2,
                PictureUrl = "mouse.png"
            }
        );

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldFilterBySearchTermAndPopulateTracker()
    {
        // Arrange
        using var context = CreateContext();
        var tracker = new AiChatTracker();
        var plugin = new ShopCatalogPlugin(context, tracker);

        // Act
        var result = await plugin.SearchProductsAsync(searchTerm: "Chuột");

        // Assert
        Assert.Contains("MX Master 3S", result);
        Assert.Single(tracker.SuggestedProducts);
        Assert.Equal("Chuột Logitech MX Master 3S", tracker.SuggestedProducts[0].Name);
        Assert.Contains(nameof(ShopCatalogPlugin.SearchProductsAsync), tracker.ToolsInvoked);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldFilterByMaxPrice()
    {
        // Arrange
        using var context = CreateContext();
        var tracker = new AiChatTracker();
        var plugin = new ShopCatalogPlugin(context, tracker);

        // Act: Tìm sản phẩm dưới 30 triệu
        var result = await plugin.SearchProductsAsync(maxPrice: 30000000m);

        // Assert: iPhone (29.99m) và Chuột (2.49m) thoả mãn, MacBook (45.99m) bị loại
        Assert.Contains("iPhone 15 Pro Max", result);
        Assert.Contains("Chuột Logitech MX Master 3S", result);
        Assert.DoesNotContain("MacBook Pro 14 M3", result);
        Assert.Equal(2, tracker.SuggestedProducts.Count);
    }

    [Fact]
    public async Task CheckProductStockAsync_ShouldReturnStockCount_WhenProductExists()
    {
        // Arrange
        using var context = CreateContext();
        var tracker = new AiChatTracker();
        var plugin = new ShopCatalogPlugin(context, tracker);

        // Act
        var result = await plugin.CheckProductStockAsync(1);

        // Assert
        Assert.Contains("hiện còn 8 chiếc trong kho", result);
        Assert.Contains(nameof(ShopCatalogPlugin.CheckProductStockAsync), tracker.ToolsInvoked);
    }

    [Fact]
    public async Task CheckProductStockAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var tracker = new AiChatTracker();
        var plugin = new ShopCatalogPlugin(context, tracker);

        // Act
        var result = await plugin.CheckProductStockAsync(999);

        // Assert
        Assert.Contains("không tồn tại", result);
    }
}

