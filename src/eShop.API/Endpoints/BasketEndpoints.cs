using eShop.Application.Basket.Commands.AddItemToBasket;
using eShop.Application.Basket.Commands.ClearBasket;
using eShop.Application.Basket.Commands.RemoveItemFromBasket;
using eShop.Application.Basket.Commands.UpdateItemQuantity;
using eShop.Application.Basket.Queries.GetBasket;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eShop.API.Endpoints;

public static class BasketEndpoints
{
    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/basket")
            .WithTags("Basket - Giỏ hàng (Redis)");

        // 1. GET /api/basket/{buyerId} - Lấy thông tin giỏ hàng
        group.MapGet("/{buyerId}", async (string buyerId, ISender sender) =>
        {
            var basket = await sender.Send(new GetBasketQuery(buyerId));
            return Results.Ok(basket);
        })
        .WithName("GetBasket")
        .WithSummary("Lấy giỏ hàng theo BuyerId (UserId hoặc SessionId)");

        // 2. POST /api/basket/{buyerId}/items - Thêm sản phẩm vào giỏ
        group.MapPost("/{buyerId}/items", async (
            string buyerId, 
            [FromBody] AddItemRequest request, 
            ISender sender) =>
        {
            var command = new AddItemToBasketCommand
            {
                BuyerId = buyerId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };

            var updatedBasket = await sender.Send(command);
            return Results.Ok(updatedBasket);
        })
        .WithName("AddItemToBasket")
        .WithSummary("Thêm một sản phẩm vào giỏ hàng (Tự động kiểm tra giá và tồn kho)");

        // 3. PUT /api/basket/{buyerId}/items/{productId} - Cập nhật số lượng
        group.MapPut("/{buyerId}/items/{productId:int}", async (
            string buyerId, 
            int productId, 
            [FromBody] UpdateQuantityRequest request, 
            ISender sender) =>
        {
            var command = new UpdateItemQuantityCommand(buyerId, productId, request.Quantity);
            var updatedBasket = await sender.Send(command);
            return Results.Ok(updatedBasket);
        })
        .WithName("UpdateItemQuantity")
        .WithSummary("Cập nhật số lượng món hàng trong giỏ (Số lượng <= 0 sẽ tự xóa)");

        // 4. DELETE /api/basket/{buyerId}/items/{productId} - Xóa một món hàng
        group.MapDelete("/{buyerId}/items/{productId:int}", async (
            string buyerId, 
            int productId, 
            ISender sender) =>
        {
            var updatedBasket = await sender.Send(new RemoveItemFromBasketCommand(buyerId, productId));
            return Results.Ok(updatedBasket);
        })
        .WithName("RemoveItemFromBasket")
        .WithSummary("Xóa một món hàng khỏi giỏ hàng");

        // 5. DELETE /api/basket/{buyerId} - Dọn sạch giỏ hàng
        group.MapDelete("/{buyerId}", async (string buyerId, ISender sender) =>
        {
            var success = await sender.Send(new ClearBasketCommand(buyerId));
            return Results.Ok(new { Success = success, Message = "Đã dọn sạch giỏ hàng." });
        })
        .WithName("ClearBasket")
        .WithSummary("Xóa trắng toàn bộ giỏ hàng của khách");

        return app;
    }
}

public record AddItemRequest(int ProductId, int Quantity);
public record UpdateQuantityRequest(int Quantity);

