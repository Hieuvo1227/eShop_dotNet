using System.Security.Claims;
using eShop.Application.Common.Interfaces;
using eShop.Application.Ordering.Queries.GetOrderById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eShop.API.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payment")
            .WithTags("Payment - Cổng thanh toán trực tuyến (VNPay)");

        // 1. POST /api/payment/vnpay-url/{orderId} - Tạo URL thanh toán VNPay cho đơn hàng
        group.MapPost("/vnpay-url/{orderId:int}", async (
            int orderId, 
            ClaimsPrincipal user, 
            ISender sender, 
            IVnPayService vnPayService, 
            HttpContext httpContext) =>
        {
            var order = await sender.Send(new GetOrderByIdQuery(orderId));
            if (order == null)
            {
                return Results.NotFound(new { Message = $"Không tìm thấy đơn hàng #{orderId}" });
            }

            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = user.IsInRole("Admin");

            if (order.BuyerId != currentUserId && !isAdmin)
            {
                return Results.NotFound(); // Chống IDOR
            }

            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var paymentUrl = vnPayService.CreatePaymentUrl(order.Id, order.TotalAmount, clientIp);

            return Results.Ok(new { OrderId = order.Id, PaymentUrl = paymentUrl });
        })
        .RequireAuthorization()
        .WithName("CreateVnPayUrl")
        .WithSummary("Tạo URL thanh toán VNPay Sandbox cho đơn hàng");

        // 2. GET /api/payment/vnpay-ipn - Webhook IPN tiếp nhận thông báo kết quả thanh toán từ VNPay Server
        group.MapGet("/vnpay-ipn", async (
            HttpRequest request, 
            IVnPayService vnPayService, 
            CancellationToken ct) =>
        {
            var queryParams = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var result = await vnPayService.ProcessIpnAsync(queryParams, ct);

            // VNPay quy định IPN phải trả về đúng định dạng JSON: { "RspCode": "xx", "Message": "yy" }
            return Results.Json(new { RspCode = result.RspCode, Message = result.Message });
        })
        .WithName("VnPayIpn")
        .WithSummary("Webhook IPN xử lý bất đồng bộ kết quả thanh toán từ máy chủ VNPay (Idempotent)");

        // 3. GET /api/payment/vnpay-return - Tiếp nhận chuyển hướng người dùng sau khi giao dịch trên cổng VNPay
        group.MapGet("/vnpay-return", (
            HttpRequest request, 
            IVnPayService vnPayService,
            IConfiguration configuration) =>
        {
            var queryParams = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var clientBaseUrl = configuration["ClientBaseUrl"] ?? "http://localhost:5256";

            if (!queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash) || string.IsNullOrWhiteSpace(vnpSecureHash))
            {
                return Results.Redirect($"{clientBaseUrl}/orders?error=missing_checksum");
            }

            var isValid = vnPayService.ValidateSignature(queryParams, vnpSecureHash);
            var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef", "0");
            var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "99");

            var isSuccess = isValid && responseCode == "00";

            return Results.Redirect($"{clientBaseUrl}/orders?orderId={txnRef}&paymentSuccess={isSuccess}&responseCode={responseCode}");
        })
        .WithName("VnPayReturn")
        .WithSummary("Tiếp nhận redirect người dùng từ VNPay và chuyển hướng về giao diện Blazor");

        return app;
    }
}
