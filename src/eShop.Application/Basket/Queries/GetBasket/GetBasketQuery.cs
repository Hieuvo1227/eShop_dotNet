using eShop.Application.Basket.DTOs;
using eShop.Application.Common.Interfaces;
using MediatR;

namespace eShop.Application.Basket.Queries.GetBasket;

public record GetBasketQuery(string BuyerId) : IRequest<CustomerBasketDto>;

public class GetBasketQueryHandler : IRequestHandler<GetBasketQuery, CustomerBasketDto>
{
    private readonly IBasketRepository _basketRepository;

    public GetBasketQueryHandler(IBasketRepository basketRepository)
    {
        _basketRepository = basketRepository;
    }

    public async Task<CustomerBasketDto> Handle(GetBasketQuery request, CancellationToken cancellationToken)
    {
        var basket = await _basketRepository.GetBasketAsync(request.BuyerId);

        if (basket == null)
        {
            return new CustomerBasketDto
            {
                BuyerId = request.BuyerId,
                Items = new List<BasketItemDto>(),
                TotalAmount = 0
            };
        }

        return new CustomerBasketDto
        {
            BuyerId = basket.BuyerId,
            Items = basket.Items.Select(i => new BasketItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                PictureUrl = i.PictureUrl
            }).ToList(),
            TotalAmount = basket.TotalAmount
        };
    }
}

