using eShop.Application.Common.Interfaces;
using eShop.Application.Ordering.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace eShop.Application.Ordering.Queries.GetAllOrders;

public record GetAllOrdersQuery(string? StatusFilter = null) : IRequest<List<OrderDto>>;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<OrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StatusFilter))
        {
            var filter = request.StatusFilter.Trim();
            query = query.Where(o => o.Status.ToString().Equals(filter));
        }

        var orders = await query
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
