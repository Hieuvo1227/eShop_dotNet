using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Catalog.Commands.UpdateProductStock;

/// <summary>
/// Command điều chỉnh số lượng tồn kho (nhập kho hoặc xuất kho).
/// </summary>
public record UpdateProductStockCommand(int ProductId, int Adjustment) : IRequest<int>;

public class UpdateProductStockCommandValidator : AbstractValidator<UpdateProductStockCommand>
{
    public UpdateProductStockCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("ProductId không hợp lệ.");
        RuleFor(x => x.Adjustment).NotEqual(0).WithMessage("Số lượng điều chỉnh không thể bằng 0.");
    }
}

public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, int>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        // Gọi phương thức nghiệp vụ của Domain Entity
        product.UpdateStock(request.Adjustment);

        await _context.SaveChangesAsync(cancellationToken);

        return product.StockQuantity;
    }
}

