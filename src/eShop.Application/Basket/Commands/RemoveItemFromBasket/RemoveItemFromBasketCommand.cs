using eShop.Application.Basket.DTOs;
using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using MediatR;

namespace eShop.Application.Basket.Commands.RemoveItemFromBasket;

public record RemoveItemFromBasketCommand(string BuyerId, int ProductId) : IRequest<CustomerBasketDto>;

public class RemoveItemFromBasketCommandHandler : IRequestHandler<RemoveItemFromBasketCommand, CustomerBasketDto>
{
    private readonly IBasketRepository _basketRepository;

    public RemoveItemFromBasketCommandHandler(IBasketRepository basketRepository)
    {
        _basketRepository = basketRepository;
    }

    public async Task<CustomerBasketDto> Handle(RemoveItemFromBasketCommand request, CancellationToken cancellationToken)
    {
        var basket = await _basketRepository.GetBasketAsync(request.BuyerId);

        if (basket == null)
        {
            throw new NotFoundException("CustomerBasket", request.BuyerId);
        }

        basket.RemoveItem(request.ProductId);

        var updatedBasket = await _basketRepository.UpdateBasketAsync(basket);

        return new CustomerBasketDto
        {
            BuyerId = updatedBasket.BuyerId,
            Items = updatedBasket.Items.Select(i => new BasketItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                PictureUrl = i.PictureUrl
            }).ToList(),
            TotalAmount = updatedBasket.TotalAmount
        };
    }
}

