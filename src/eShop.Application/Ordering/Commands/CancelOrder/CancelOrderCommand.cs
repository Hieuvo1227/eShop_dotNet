using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Ordering.Commands.CancelOrder;

public record CancelOrderCommand(int OrderId) : IRequest<bool>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public CancelOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        // 1. Chuyển trạng thái Order sang Cancelled
        order.SetCancelledStatus();

        // 2. Hoàn trả lại số lượng tồn kho cho các sản phẩm
        var productIds = order.OrderItems.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var item in order.OrderItems)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
            {
                product.UpdateStock(item.Units); // Hoàn tồn kho
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

