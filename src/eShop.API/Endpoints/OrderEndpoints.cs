using System.Security.Claims;
using eShop.Application.Ordering.Commands.CancelOrder;
using eShop.Application.Ordering.Commands.CreateOrder;
using eShop.Application.Ordering.DTOs;
using eShop.Application.Ordering.Queries.GetOrderById;
using eShop.Application.Ordering.Queries.GetOrdersByBuyerId;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eShop.API.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders - Đơn hàng (DDD)")
            .RequireAuthorization(); // Yêu cầu xác thực JWT cho toàn bộ luồng đơn hàng

        // 1. POST /api/orders - Đặt hàng (BuyerId lấy trực tiếp từ Token)
        group.MapPost("/", async (
            [FromBody] CreateOrderRequest request, 
            ClaimsPrincipal user, 
            ISender sender) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.Unauthorized();
            }

            var command = new CreateOrderCommand
            {
                BuyerId = buyerId,
                ShippingAddress = request.ShippingAddress,
                PaymentMethod = request.PaymentMethod
            };

            var order = await sender.Send(command);
            return Results.Created($"/api/orders/{order.Id}", order);
        })
        .WithName("CreateOrder")
        .WithSummary("Tạo đơn hàng mới từ giỏ hàng (BuyerId trích xuất an toàn từ Token)");

        // 2. GET /api/orders/me - Lấy lịch sử đơn hàng của chính tài khoản đang đăng nhập
        group.MapGet("/me", async (ClaimsPrincipal user, ISender sender) =>
        {
            var buyerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.Unauthorized();
            }

            var orders = await sender.Send(new GetOrdersByBuyerIdQuery(buyerId));
            return Results.Ok(orders);
        })
        .WithName("GetMyOrders")
        .WithSummary("Lấy danh sách lịch sử đơn hàng của chính người dùng hiện tại");

        // 3. GET /api/orders/{id} - Lấy chi tiết đơn hàng (Chỉ chủ sở hữu hoặc Admin, trả về 404 nếu không khớp)
        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, ISender sender) =>
        {
            var order = await sender.Send(new GetOrderByIdQuery(id));
            if (order == null)
            {
                return Results.NotFound();
            }

            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = user.IsInRole("Admin");

            // Chống IDOR: Trả về 404 thay vì 403 để không để lộ sự tồn tại của đơn hàng người khác
            if (order.BuyerId != currentUserId && !isAdmin)
            {
                return Results.NotFound();
            }

            return Results.Ok(order);
        })
        .WithName("GetOrderById")
        .WithSummary("Lấy thông tin chi tiết một đơn hàng theo Id (Bảo vệ chống IDOR - 404 nếu không thuộc quyền sở hữu)");

        // 4. PUT /api/orders/{id}/cancel - Hủy đơn hàng (Chỉ chủ sở hữu hoặc Admin)
        group.MapPut("/{id:int}/cancel", async (int id, ClaimsPrincipal user, ISender sender) =>
        {
            var order = await sender.Send(new GetOrderByIdQuery(id));
            if (order == null)
            {
                return Results.NotFound();
            }

            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = user.IsInRole("Admin");

            if (order.BuyerId != currentUserId && !isAdmin)
            {
                return Results.NotFound();
            }

            var success = await sender.Send(new CancelOrderCommand(id));
            return Results.Ok(new { Success = success, Message = $"Đơn hàng #{id} đã được hủy và hoàn trả tồn kho." });
        })
        .WithName("CancelOrder")
        .WithSummary("Hủy đơn hàng và tự động hoàn trả số lượng hàng về kho Catalog");

        return app;
    }
}

public record CreateOrderRequest(AddressDto ShippingAddress, string PaymentMethod = "COD");
