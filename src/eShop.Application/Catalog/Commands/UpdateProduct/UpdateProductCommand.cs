using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Catalog.Commands.UpdateProduct;

public record UpdateProductCommand : IRequest<bool>
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SKU { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string? PictureUrl { get; init; }
    public int BrandId { get; init; }
    public int CategoryId { get; init; }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id sản phẩm không hợp lệ.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("Tên sản phẩm không được quá 200 ký tự.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0.");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Số lượng kho không thể là số âm.");
        RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("Vui lòng chọn Thương hiệu hợp lệ.");
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Vui lòng chọn Ngành hàng hợp lệ.");
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        var brandExists = await _context.Brands.AnyAsync(b => b.Id == request.BrandId, cancellationToken);
        if (!brandExists)
        {
            throw new NotFoundException("Brand", request.BrandId);
        }

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.SKU = string.IsNullOrWhiteSpace(request.SKU) ? product.SKU : request.SKU.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.PictureUrl = request.PictureUrl?.Trim();
        product.BrandId = request.BrandId;
        product.CategoryId = request.CategoryId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

