using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using eShop.Application.Ordering.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Ordering.Queries.GetOrderById;

public record GetOrderByIdQuery(int Id) : IRequest<OrderDto>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", request.Id);
        }

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

