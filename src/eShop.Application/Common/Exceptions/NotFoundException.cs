namespace eShop.Application.Common.Exceptions;

/// <summary>
/// Exception ném ra khi không tìm thấy thực thể được yêu cầu trong database.
/// Sẽ được GlobalExceptionHandler bắt và chuyển thành mã HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Không tìm thấy đối tượng '{entityName}' với khóa ({key}).")
    {
    }
}

