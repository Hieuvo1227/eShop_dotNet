namespace eShop.Application.Common.Interfaces;

public record VnPayIpnResult(string RspCode, string Message);

public interface IVnPayService
{
    /// <summary>
    /// Tạo URL cổng thanh toán VNPay Sandbox với chữ ký HMAC-SHA512.
    /// </summary>
    string CreatePaymentUrl(int orderId, decimal amount, string clientIp, string? orderInfo = null);

    /// <summary>
    /// Xử lý Webhook IPN từ VNPay server: kiểm tra chữ ký, đối chiếu số tiền,
    /// và cập nhật trạng thái đơn hàng thành Paid một cách Idempotent.
    /// </summary>
    Task<VnPayIpnResult> ProcessIpnAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra tính hợp lệ của chữ ký checksum từ VNPay trả về.
    /// </summary>
    bool ValidateSignature(IDictionary<string, string> queryParameters, string inputHash);
}
