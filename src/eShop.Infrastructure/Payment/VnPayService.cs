using eShop.Application.Common.Interfaces;
using eShop.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eShop.Infrastructure.Payment;

/// <summary>
/// Dịch vụ kết nối và xử lý giao dịch cổng thanh toán điện tử VNPay Sandbox.
/// Đảm bảo xác thực chữ ký số HMAC-SHA512, đối chiếu số tiền và xử lý Idempotent cho IPN Webhook.
/// </summary>
public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<VnPayService> _logger;

    public VnPayService(
        IConfiguration configuration,
        IApplicationDbContext context,
        ILogger<VnPayService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public string CreatePaymentUrl(int orderId, decimal amount, string clientIp, string? orderInfo = null)
    {
        var tmnCode = _configuration["VnPay:TmnCode"] ?? "ESHOPDEMO";
        var hashSecret = _configuration["VnPay:HashSecret"] ?? "VNPAYHASHSECRET2026TESTDEMOKEYFORDEVELOPMENTONLY!";
        var baseUrl = _configuration["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var returnUrl = _configuration["VnPay:ReturnUrl"] ?? "http://localhost:5000/api/payment/vnpay-return";

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", tmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
        vnpay.AddRequestData("vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp);
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", string.IsNullOrWhiteSpace(orderInfo) ? $"Thanh toan don hang #{orderId} tai eShop" : orderInfo);
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_TxnRef", orderId.ToString());

        var paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);
        _logger.LogInformation("Đã khởi tạo VNPay Payment URL cho Đơn hàng #{OrderId} với số tiền {Amount} VND", orderId, amount);

        return paymentUrl;
    }

    public async Task<VnPayIpnResult> ProcessIpnAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default)
    {
        var hashSecret = _configuration["VnPay:HashSecret"] ?? "VNPAYHASHSECRET2026TESTDEMOKEYFORDEVELOPMENTONLY!";

        if (!queryParameters.TryGetValue("vnp_SecureHash", out var vnpSecureHash) || string.IsNullOrWhiteSpace(vnpSecureHash))
        {
            _logger.LogWarning("[VNPay IPN] Thiếu trường chữ ký vnp_SecureHash");
            return new VnPayIpnResult("97", "Invalid Checksum");
        }

        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in queryParameters)
        {
            vnpay.AddResponseData(key, value);
        }

        // 1. Kiểm tra tính toàn vẹn chữ ký HMAC-SHA512
        var isSignatureValid = vnpay.ValidateSignature(vnpSecureHash, hashSecret);
        if (!isSignatureValid)
        {
            _logger.LogWarning("[VNPay IPN] Chữ ký không hợp lệ từ VNPay Server");
            return new VnPayIpnResult("97", "Invalid Checksum");
        }

        // 2. Trích xuất thông tin giao dịch
        var txnRef = vnpay.GetResponseData("vnp_TxnRef");
        if (!int.TryParse(txnRef, out var orderId))
        {
            _logger.LogWarning("[VNPay IPN] Mã đơn hàng vnp_TxnRef không hợp lệ: {TxnRef}", txnRef);
            return new VnPayIpnResult("01", "Order Not Found");
        }

        var amountStr = vnpay.GetResponseData("vnp_Amount");
        var amountPaid = long.TryParse(amountStr, out var rawAmount) ? rawAmount / 100m : 0m;
        var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var transactionNo = vnpay.GetResponseData("vnp_TransactionNo");

        // 3. Đối chiếu đơn hàng trong Database
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("[VNPay IPN] Đơn hàng #{OrderId} không tồn tại trong hệ thống", orderId);
            return new VnPayIpnResult("01", "Order Not Found");
        }

        // 4. Đối chiếu số tiền giao dịch
        if (order.TotalAmount != amountPaid)
        {
            _logger.LogWarning("[VNPay IPN] Số tiền thanh toán ({AmountPaid}) không khớp với giá trị đơn hàng ({OrderTotal})", amountPaid, order.TotalAmount);
            return new VnPayIpnResult("04", "Invalid Amount");
        }

        // 5. Kiểm tra tính Idempotent: Nếu đơn hàng đã được xác nhận thanh toán trước đó
        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("[VNPay IPN] Đơn hàng #{OrderId} đã được xác nhận thanh toán trước đó (Idempotent response)", orderId);
            return new VnPayIpnResult("02", "Order already confirmed");
        }

        // 6. Xử lý kết quả giao dịch
        if (responseCode == "00")
        {
            order.SetPaidStatus(transactionNo);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ [VNPay IPN] Xác nhận thanh toán thành công cho Đơn hàng #{OrderId} (Mã GD VNPay: {TxnNo})", orderId, transactionNo);
            return new VnPayIpnResult("00", "Confirm Success");
        }
        else
        {
            _logger.LogWarning("❌ [VNPay IPN] Giao dịch không thành công cho Đơn hàng #{OrderId} (Mã phản hồi: {ResponseCode})", orderId, responseCode);
            return new VnPayIpnResult("00", "Payment Failed Recorded");
        }
    }

    public bool ValidateSignature(IDictionary<string, string> queryParameters, string inputHash)
    {
        var hashSecret = _configuration["VnPay:HashSecret"] ?? "VNPAYHASHSECRET2026TESTDEMOKEYFORDEVELOPMENTONLY!";
        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in queryParameters)
        {
            vnpay.AddResponseData(key, value);
        }
        return vnpay.ValidateSignature(inputHash, hashSecret);
    }
}

