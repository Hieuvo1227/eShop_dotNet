using eShop.Application.Basket.DTOs;
using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace eShop.Application.Basket.Commands.UpdateItemQuantity;

public record UpdateItemQuantityCommand(string BuyerId, int ProductId, int NewQuantity) : IRequest<CustomerBasketDto>;

public class UpdateItemQuantityCommandValidator : AbstractValidator<UpdateItemQuantityCommand>
{
    public UpdateItemQuantityCommandValidator()
    {
        RuleFor(x => x.BuyerId).NotEmpty().WithMessage("BuyerId không được để trống.");
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("ProductId không hợp lệ.");
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0).WithMessage("Số lượng không thể là số âm.");
    }
}

public class UpdateItemQuantityCommandHandler : IRequestHandler<UpdateItemQuantityCommand, CustomerBasketDto>
{
    private readonly IBasketRepository _basketRepository;

    public UpdateItemQuantityCommandHandler(IBasketRepository basketRepository)
    {
        _basketRepository = basketRepository;
    }

    public async Task<CustomerBasketDto> Handle(UpdateItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var basket = await _basketRepository.GetBasketAsync(request.BuyerId);

        if (basket == null)
        {
            throw new NotFoundException("CustomerBasket", request.BuyerId);
        }

        basket.UpdateQuantity(request.ProductId, request.NewQuantity);

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

