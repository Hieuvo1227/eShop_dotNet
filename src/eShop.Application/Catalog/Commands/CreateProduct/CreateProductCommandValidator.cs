using eShop.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Catalog.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự.");

        RuleFor(v => v.SKU)
            .NotEmpty().WithMessage("Mã SKU không được để trống.")
            .MaximumLength(50).WithMessage("Mã SKU không được vượt quá 50 ký tự.")
            .MustAsync(BeUniqueSKU).WithMessage("Mã SKU này đã tồn tại trong hệ thống.");

        RuleFor(v => v.Price)
            .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0.");

        RuleFor(v => v.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng tồn kho không được là số âm.");

        RuleFor(v => v.BrandId)
            .GreaterThan(0).WithMessage("Thương hiệu không hợp lệ.")
            .MustAsync(BrandExists).WithMessage("Thương hiệu được chọn không tồn tại.");

        RuleFor(v => v.CategoryId)
            .GreaterThan(0).WithMessage("Danh mục không hợp lệ.")
            .MustAsync(CategoryExists).WithMessage("Danh mục được chọn không tồn tại.");
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken cancellationToken)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        return !await _context.Products
            .AnyAsync(p => p.SKU == normalizedSku, cancellationToken);
    }

    private async Task<bool> BrandExists(int brandId, CancellationToken cancellationToken)
    {
        return await _context.Brands
            .AnyAsync(b => b.Id == brandId, cancellationToken);
    }

    private async Task<bool> CategoryExists(int categoryId, CancellationToken cancellationToken)
    {
        return await _context.Categories
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
}

