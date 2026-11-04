using eShop.Application.Common.Interfaces;
using eShop.Application.Ordering.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Ordering.Queries.GetOrdersByBuyerId;

public record GetOrdersByBuyerIdQuery(string BuyerId) : IRequest<List<OrderDto>>;

public class GetOrdersByBuyerIdQueryHandler : IRequestHandler<GetOrdersByBuyerIdQuery, List<OrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetOrdersByBuyerIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderDto>> Handle(GetOrdersByBuyerIdQuery request, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Where(o => o.BuyerId == request.BuyerId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(order => new OrderDto
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
        }).ToList();
    }
}

