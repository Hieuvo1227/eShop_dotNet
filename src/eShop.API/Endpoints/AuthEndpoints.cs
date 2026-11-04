using System.Security.Claims;
using eShop.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eShop.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth")
            .WithTags("Auth - Xác thực & Phân quyền");

        // 1. POST /api/auth/login - Đăng nhập tài khoản
        authGroup.MapPost("/login", async ([FromBody] LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request.Email, request.Password);
            if (result == null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Đăng nhập thất bại",
                    detail: "Email hoặc mật khẩu không chính xác."
                );
            }
            return Results.Ok(result);
        })
        .WithName("Login")
        .WithSummary("Đăng nhập hệ thống (Mẫu Admin: admin@eshop.com/Admin@123 | Mẫu User: user@eshop.com/User@123)");

        // 2. POST /api/auth/register - Đăng ký tài khoản mới (Vai trò Customer)
        authGroup.MapPost("/register", async ([FromBody] RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request.Email, request.Password, request.FullName);
            return Results.Created("/api/auth/me", result);
        })
        .WithName("Register")
        .WithSummary("Đăng ký tài khoản người dùng mới (Mặc định quyền Customer)");

        // 3. GET /api/auth/me - Xem thông tin tài khoản hiện tại (Yêu cầu đăng nhập - Bất kỳ Role nào)
        authGroup.MapGet("/me", (ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = user.FindFirstValue(ClaimTypes.Email);
            var name = user.FindFirstValue(ClaimTypes.Name);
            var role = user.FindFirstValue(ClaimTypes.Role);

            return Results.Ok(new
            {
                Id = userId,
                Email = email,
                FullName = name,
                Role = role,
                IsAuthenticated = user.Identity?.IsAuthenticated ?? false
            });
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Lấy thông tin tài khoản đang đăng nhập (Yêu cầu JWT Token - Trả về 401 nếu thiếu Token)");

        // 4. Nhóm API dành riêng cho Quản trị viên (Admin-Only API)
        var adminGroup = app.MapGroup("/api/admin")
            .WithTags("Admin - Quản trị hệ thống (Role Admin Only)")
            .RequireAuthorization("AdminOnly");

        // GET /api/admin/metrics - Xem báo cáo tổng quan hệ thống
        adminGroup.MapGet("/metrics", async (IApplicationDbContext dbContext, ClaimsPrincipal user) =>
        {
            var totalProducts = await dbContext.Products.CountAsync();
            var totalOrders = await dbContext.Orders.CountAsync();
            var orders = await dbContext.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking()
                .ToListAsync();
            var totalRevenue = orders.Sum(o => o.TotalAmount);

            return Results.Ok(new
            {
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                RequestedBy = user.FindFirstValue(ClaimTypes.Email),
                SystemStatus = "All systems operational",
                ServerTimeUtc = DateTime.UtcNow
            });
        })
        .WithName("GetAdminMetrics")
        .WithSummary("Xem số liệu tài chính & sản phẩm nội bộ (Yêu cầu quyền Admin - Trả về 403 nếu quyền Customer)");

        // GET /api/admin/orders - Lấy toàn bộ đơn hàng của tất cả khách hàng
        adminGroup.MapGet("/orders", async ([FromQuery] string? status, MediatR.ISender sender) =>
        {
            var orders = await sender.Send(new eShop.Application.Ordering.Queries.GetAllOrders.GetAllOrdersQuery(status));
            return Results.Ok(orders);
        })
        .WithName("GetAdminOrders")
        .WithSummary("Xem danh sách toàn bộ đơn hàng trong hệ thống (Yêu cầu quyền Admin)");

        // PUT /api/admin/orders/{id}/status - Cập nhật trạng thái đơn hàng
        adminGroup.MapPut("/orders/{id:int}/status", async (
            int id, 
            [FromBody] UpdateOrderStatusRequest request, 
            MediatR.ISender sender) =>
        {
            var updatedOrder = await sender.Send(new eShop.Application.Ordering.Commands.UpdateOrderStatus.UpdateOrderStatusCommand(
                id, 
                request.Status, 
                request.TransactionId));
            return Results.Ok(updatedOrder);
        })
        .WithName("UpdateAdminOrderStatus")
        .WithSummary("Cập nhật trạng thái đơn hàng (Shipped, Paid, Completed, Cancelled - Yêu cầu quyền Admin)");

        return app;
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FullName);
public record UpdateOrderStatusRequest(string Status, string? TransactionId);
