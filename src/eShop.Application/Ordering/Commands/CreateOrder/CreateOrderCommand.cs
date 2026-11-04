using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using eShop.Application.Common.Models.Events;
using eShop.Application.Ordering.DTOs;
using eShop.Domain.Entities.Ordering;
using eShop.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = eShop.Application.Common.Exceptions.ValidationException;

namespace eShop.Application.Ordering.Commands.CreateOrder;

/// <summary>
/// Command Đặt Hàng (Checkout): Chuyển đổi giỏ hàng từ Redis thành đơn hàng chính thức trong Database.
/// </summary>
public record CreateOrderCommand : IRequest<OrderDto>
{
    public string BuyerId { get; init; } = string.Empty;
    public AddressDto ShippingAddress { get; init; } = new();
    public string PaymentMethod { get; init; } = "COD";
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.BuyerId)
            .NotEmpty().WithMessage("BuyerId không được để trống.");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Địa chỉ giao hàng không được để trống.");

        RuleFor(x => x.ShippingAddress.Street)
            .NotEmpty().WithMessage("Tên đường/Số nhà không được để trống.");

        RuleFor(x => x.ShippingAddress.District)
            .NotEmpty().WithMessage("Quận/Huyện không được để trống.");

        RuleFor(x => x.ShippingAddress.City)
            .NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.");
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IBasketRepository _basketRepository;
    private readonly IEventBus _eventBus;
    private readonly IVnPayService? _vnPayService;

    public CreateOrderCommandHandler(
        IApplicationDbContext context, 
        IBasketRepository basketRepository,
        IEventBus eventBus,
        IVnPayService? vnPayService = null)
    {
        _context = context;
        _basketRepository = basketRepository;
        _eventBus = eventBus;
        _vnPayService = vnPayService;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Đọc giỏ hàng từ Redis
        var basket = await _basketRepository.GetBasketAsync(request.BuyerId);

        if (basket == null || !basket.Items.Any())
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("Basket", "Giỏ hàng của bạn đang trống, không thể tạo đơn hàng.")
            });
        }

        // 2. Chuyển đổi AddressDto sang Domain Value Object
        var address = new Address(
            request.ShippingAddress.Street.Trim(),
            request.ShippingAddress.District.Trim(),
            request.ShippingAddress.City.Trim(),
            request.ShippingAddress.ZipCode?.Trim(),
            string.IsNullOrWhiteSpace(request.ShippingAddress.Country) ? "Vietnam" : request.ShippingAddress.Country.Trim());

        // 3. Khởi tạo Aggregate Root Order
        var order = new Order(request.BuyerId, address, request.PaymentMethod);

        // 4. Kiểm tra tồn kho và trừ số lượng sản phẩm bằng Atomic Update trong Database Transaction
        var isRelational = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        if (isRelational)
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var productIds = basket.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in basket.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                if (product == null)
                {
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    throw new NotFoundException("Product", item.ProductId);
                }

                if (isRelational)
                {
                    // Cập nhật nguyên tử trực tiếp chống overselling (Race condition)
                    var affected = await _context.Products
                        .Where(p => p.Id == item.ProductId && p.StockQuantity >= item.Quantity)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - item.Quantity), cancellationToken);

                    if (affected == 0)
                    {
                        if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                        throw new InsufficientStockException(item.ProductId, product.Name);
                    }
                }
                else
                {
                    // Fallback cho kiểm thử In-Memory không hỗ trợ SQL ExecuteUpdate
                    if (product.StockQuantity < item.Quantity)
                    {
                        throw new InsufficientStockException(item.ProductId, product.Name);
                    }
                    product.UpdateStock(-item.Quantity);
                }

                // Thêm vào Order Aggregate
                order.AddOrderItem(
                    product.Id, 
                    product.Name, 
                    product.Price, 
                    item.Quantity, 
                    product.PictureUrl);
            }

            // 5. Lưu Đơn hàng vào Database trong cùng transaction
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }

        // 6. Xóa giỏ hàng trên Redis sau khi đặt hàng thành công
        await _basketRepository.DeleteBasketAsync(request.BuyerId);

        // 7. Phát đi Integration Event lên Message Broker (RabbitMQ / MassTransit)
        // Các tác vụ nền (gửi email, xuất hóa đơn, thông báo kho) sẽ được các Consumer xử lý bất đồng bộ
        var integrationEvent = new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            BuyerId = order.BuyerId,
            TotalAmount = order.TotalAmount,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = order.OrderItems.Select(i => new OrderItemIntegrationDto(
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Units)).ToList()
        };

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        // 8. Tạo Payment URL nếu phương thức thanh toán là VNPay
        string? paymentUrl = null;
        if (_vnPayService != null && order.PaymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase))
        {
            paymentUrl = _vnPayService.CreatePaymentUrl(order.Id, order.TotalAmount, "127.0.0.1");
        }

        // 9. Trả về OrderDto
        return new OrderDto
        {
            Id = order.Id,
            BuyerId = order.BuyerId,
            OrderDateUtc = order.CreatedAtUtc,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod,
            PaymentTransactionId = order.PaymentTransactionId,
            PaymentUrl = paymentUrl,
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

