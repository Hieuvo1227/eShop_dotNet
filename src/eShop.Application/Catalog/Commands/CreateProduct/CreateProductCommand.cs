using eShop.Application.Common.Interfaces;
using eShop.Domain.Entities;
using MediatR;

namespace eShop.Application.Catalog.Commands.CreateProduct;

/// <summary>
/// Command thêm mới một sản phẩm vào hệ thống.
/// Trả về Id của sản phẩm vừa tạo.
/// </summary>
public record CreateProductCommand : IRequest<int>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SKU { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string? PictureUrl { get; init; }
    public int BrandId { get; init; }
    public int CategoryId { get; init; }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            SKU = request.SKU.Trim().ToUpperInvariant(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            PictureUrl = request.PictureUrl,
            BrandId = request.BrandId,
            CategoryId = request.CategoryId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

