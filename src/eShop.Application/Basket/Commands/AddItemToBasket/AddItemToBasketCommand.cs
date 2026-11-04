using eShop.Application.Basket.DTOs;
using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using eShop.Domain.Entities.Basket;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = eShop.Application.Common.Exceptions.ValidationException;

namespace eShop.Application.Basket.Commands.AddItemToBasket;

public record AddItemToBasketCommand : IRequest<CustomerBasketDto>
{
    public string BuyerId { get; init; } = string.Empty;
    public int ProductId { get; init; }
    public int Quantity { get; init; } = 1;
}

public class AddItemToBasketCommandValidator : AbstractValidator<AddItemToBasketCommand>
{
    public AddItemToBasketCommandValidator()
    {
        RuleFor(x => x.BuyerId)
            .NotEmpty().WithMessage("BuyerId không được để trống.");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId không hợp lệ.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Số lượng thêm vào giỏ phải lớn hơn 0.");
    }
}

public class AddItemToBasketCommandHandler : IRequestHandler<AddItemToBasketCommand, CustomerBasketDto>
{
    private readonly IBasketRepository _basketRepository;
    private readonly IApplicationDbContext _context;

    public AddItemToBasketCommandHandler(
        IBasketRepository basketRepository, 
        IApplicationDbContext context)
    {
        _basketRepository = basketRepository;
        _context = context;
    }

    public async Task<CustomerBasketDto> Handle(
        AddItemToBasketCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Kiểm tra sản phẩm trong Database Catalog
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        // 2. Kiểm tra tồn kho có đủ không
        if (product.StockQuantity < request.Quantity)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    "Quantity", 
                    $"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} món trong kho.")
            });
        }

        // 3. Lấy giỏ hàng từ Redis (hoặc tạo mới nếu chưa có)
        var basket = await _basketRepository.GetBasketAsync(request.BuyerId) 
            ?? new CustomerBasket(request.BuyerId);

        // 4. Thêm món vào giỏ
        basket.AddItem(
            product.Id, 
            product.Name, 
            product.Price, 
            request.Quantity, 
            product.PictureUrl);

        // 5. Lưu cập nhật vào Redis
        var updatedBasket = await _basketRepository.UpdateBasketAsync(basket);

        // 6. Trả về DTO
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

