using eShop.Application.Common.Interfaces;
using MediatR;

namespace eShop.Application.Basket.Commands.ClearBasket;

public record ClearBasketCommand(string BuyerId) : IRequest<bool>;

public class ClearBasketCommandHandler : IRequestHandler<ClearBasketCommand, bool>
{
    private readonly IBasketRepository _basketRepository;

    public ClearBasketCommandHandler(IBasketRepository basketRepository)
    {
        _basketRepository = basketRepository;
    }

    public async Task<bool> Handle(ClearBasketCommand request, CancellationToken cancellationToken)
    {
        return await _basketRepository.DeleteBasketAsync(request.BuyerId);
    }
}

