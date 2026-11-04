using eShop.Application.Ordering.Commands.CancelOrder;
using eShop.Application.Ordering.Commands.CreateOrder;
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
            .WithTags("Orders - Đơn hàng (DDD)");

        // 1. POST /api/orders - Đặt hàng (Checkout từ giỏ hàng Redis sang Database)
        group.MapPost("/", async ([FromBody] CreateOrderCommand command, ISender sender) =>
        {
            var order = await sender.Send(command);
            return Results.Created($"/api/orders/{order.Id}", order);
        })
        .WithName("CreateOrder")
        .WithSummary("Tạo đơn hàng mới từ giỏ hàng hiện tại (Tự động trừ kho và dọn giỏ hàng)");

        // 2. GET /api/orders/{id} - Lấy chi tiết đơn hàng
        group.MapGet("/{id:int}", async (int id, ISender sender) =>
        {
            var order = await sender.Send(new GetOrderByIdQuery(id));
            return Results.Ok(order);
        })
        .WithName("GetOrderById")
        .WithSummary("Lấy thông tin chi tiết một đơn hàng theo Id");

        // 3. GET /api/orders/buyer/{buyerId} - Lấy lịch sử mua hàng của khách
        group.MapGet("/buyer/{buyerId}", async (string buyerId, ISender sender) =>
        {
            var orders = await sender.Send(new GetOrdersByBuyerIdQuery(buyerId));
            return Results.Ok(orders);
        })
        .WithName("GetOrdersByBuyer")
        .WithSummary("Lấy danh sách lịch sử đơn hàng của một khách hàng");

        // 4. PUT /api/orders/{id}/cancel - Hủy đơn hàng
        group.MapPut("/{id:int}/cancel", async (int id, ISender sender) =>
        {
            var success = await sender.Send(new CancelOrderCommand(id));
            return Results.Ok(new { Success = success, Message = $"Đơn hàng #{id} đã được hủy và hoàn trả tồn kho." });
        })
        .WithName("CancelOrder")
        .WithSummary("Hủy đơn hàng và tự động hoàn trả số lượng hàng về kho Catalog");

        return app;
    }
}

