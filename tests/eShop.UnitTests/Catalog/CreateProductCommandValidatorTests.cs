using eShop.Application.Catalog.Commands.CreateProduct;
using eShop.Application.Common.Interfaces;
using eShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace eShop.UnitTests.Catalog;

public class CreateProductCommandValidatorTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly CreateProductCommandValidator _validator;

    public CreateProductCommandValidatorTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();

        // Thiết lập Mock DB cho Brand và Category hợp lệ
        var brands = new List<Brand> { new() { Id = 1, Name = "Apple" } }.AsQueryable();
        var mockBrandsDbSet = CreateMockDbSet(brands);
        _mockContext.Setup(c => c.Brands).Returns(mockBrandsDbSet.Object);

        var categories = new List<Category> { new() { Id = 1, Name = "Smartphones" } }.AsQueryable();
        var mockCategoriesDbSet = CreateMockDbSet(categories);
        _mockContext.Setup(c => c.Categories).Returns(mockCategoriesDbSet.Object);

        var products = new List<Product>
        {
            new() { Id = 1, Name = "iPhone 15", SKU = "IP15-EXISTING", BrandId = 1, CategoryId = 1, Price = 20000000 }
        }.AsQueryable();
        var mockProductsDbSet = CreateMockDbSet(products);
        _mockContext.Setup(c => c.Products).Returns(mockProductsDbSet.Object);

        _validator = new CreateProductCommandValidator(_mockContext.Object);
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "iPhone 16 Pro",
            Description = "Điện thoại mới nhất",
            SKU = "IP16P-NEW",
            Price = 28990000m,
            StockQuantity = 50,
            BrandId = 1,
            CategoryId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "",
            SKU = "IP16P-NEW",
            Price = 28990000m,
            StockQuantity = 50,
            BrandId = 1,
            CategoryId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPriceIsNegative()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "iPhone 16 Pro",
            SKU = "IP16P-NEW",
            Price = -500000m,
            StockQuantity = 50,
            BrandId = 1,
            CategoryId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenStockQuantityIsNegative()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "iPhone 16 Pro",
            SKU = "IP16P-NEW",
            Price = 28000000m,
            StockQuantity = -10,
            BrandId = 1,
            CategoryId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StockQuantity");
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(data.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

        return mockSet;
    }
}

// Helper classes để Mock EF Core IAsyncQueryable
internal class TestAsyncQueryProvider<TEntity> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = ((IQueryProvider)this).Execute(expression);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public T Current => _inner.Current;
}

