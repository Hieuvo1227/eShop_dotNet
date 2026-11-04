using System.Security.Claims;
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
            .WithTags("Basket - Giỏ hàng (Redis)")
            .RequireAuthorization(); // Yêu cầu xác thực JWT để bảo vệ giỏ hàng người dùng chống IDOR

        // 1. GET /api/basket - Lấy thông tin giỏ hàng của chính tài khoản đăng nhập
        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.Unauthorized();
            }

            var basket = await sender.Send(new GetBasketQuery(buyerId));
            return Results.Ok(basket);
        })
        .WithName("GetBasket")
        .WithSummary("Lấy giỏ hàng theo User Id xác thực từ Token");

        // 2. POST /api/basket/items - Thêm sản phẩm vào giỏ
        group.MapPost("/items", async (
            [FromBody] AddItemRequest request, 
            ClaimsPrincipal user, 
            ISender sender) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.Unauthorized();
            }

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
        .WithSummary("Thêm sản phẩm vào giỏ hàng (BuyerId trích xuất an toàn từ Token)");

        // 3. PUT /api/basket/items/{productId} - Cập nhật số lượng
        group.MapPut("/items/{productId:int}", async (
            int productId, 
            [FromBody] UpdateQuantityRequest request, 
            ClaimsPrincipal user, 
            ISender sender) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.Unauthorized();
            }

            var command = new UpdateItemQuantityCommand(buyerId, productId, request.Quantity);
            var updatedBasket = await sender.Send(command);
            return Results.Ok(updatedBasket);
        })
        .WithName("UpdateItemQuantity")
        .WithSummary("Cập nhật số lượng món hàng trong giỏ");

        // 4. DELETE /api/basket/items/{productId} - Xóa một món hàng
        group.MapDelete("/items/{productId:int}", async (
            int productId, 
            ClaimsPrincipal user, 
            ISender sender) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.Unauthorized();
            }

            var updatedBasket = await sender.Send(new RemoveItemFromBasketCommand(buyerId, productId));
            return Results.Ok(updatedBasket);
        })
        .WithName("RemoveItemFromBasket")
        .WithSummary("Xóa một món hàng khỏi giỏ hàng");

        // 5. DELETE /api/basket - Dọn sạch giỏ hàng
        group.MapDelete("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.Unauthorized();
            }

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
