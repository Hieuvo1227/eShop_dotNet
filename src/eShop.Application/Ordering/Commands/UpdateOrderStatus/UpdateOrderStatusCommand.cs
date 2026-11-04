using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using eShop.Application.Ordering.DTOs;
using eShop.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Ordering.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(int OrderId, string NewStatus, string? TransactionId = null) : IRequest<OrderDto>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateOrderStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        switch (request.NewStatus.Trim().ToLowerInvariant())
        {
            case "paid":
                order.SetPaidStatus(request.TransactionId);
                break;

            case "shipped":
                order.SetShippedStatus();
                break;

            case "completed":
                order.SetCompletedStatus();
                break;

            case "cancelled":
                order.SetCancelledStatus();
                // Hoàn lại tồn kho nếu hủy đơn
                var productIds = order.OrderItems.Select(i => i.ProductId).ToList();
                var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync(cancellationToken);
                foreach (var item in order.OrderItems)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        product.UpdateStock(item.Units);
                    }
                }
                break;

            default:
                throw new ArgumentException($"Trạng thái '{request.NewStatus}' không hợp lệ.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new OrderDto
        {
            Id = order.Id,
            BuyerId = order.BuyerId,
            OrderDateUtc = order.CreatedAtUtc,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod,
            PaymentTransactionId = order.PaymentTransactionId,
            TotalAmount = order.TotalAmount,
            ShippingAddress = new AddressDto
            {
                Street = order.ShippingAddress.Street,
                District = order.ShippingAddress.District,
                City = order.ShippingAddress.City,
                ZipCode = order.ShippingAddress.ZipCode,
                Country = order.ShippingAddress.Country
            },
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Units = i.Units,
                PictureUrl = i.PictureUrl
            }).ToList()
        };
    }
}
