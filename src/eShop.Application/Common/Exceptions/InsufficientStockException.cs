namespace eShop.Application.Common.Exceptions;

/// <summary>
/// Ngoại lệ ném ra khi sản phẩm không đủ số lượng tồn kho để thỏa mãn đơn đặt hàng.
/// Tự động được GlobalExceptionHandler chuyển thành mã lỗi HTTP 409 Conflict (RFC 7231).
/// </summary>
public class InsufficientStockException : Exception
{
    public int ProductId { get; }
    public string? ProductName { get; }

    public InsufficientStockException(int productId, string? productName = null)
        : base($"Sản phẩm {(string.IsNullOrWhiteSpace(productName) ? "" : $"'{productName}' ")}#{productId} không còn đủ số lượng tồn kho để đặt hàng.")
    {
        ProductId = productId;
        ProductName = productName;
    }
}
