namespace eShop.Application.Common.Interfaces;

/// <summary>
/// DTO phản hồi sau khi xác thực thành công, chứa JWT Token và thông tin cơ bản của User.
/// </summary>
public record AuthResponseDto(
    string Id,
    string Token,
    string Email,
    string FullName,
    string Role,
    int ExpiresInSeconds
);

/// <summary>
/// Giao diện dịch vụ Xác thực & Phân quyền (Authentication & Authorization).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng nhập bằng Email và Mật khẩu. Trả về Token nếu đúng, null nếu sai thông tin.
    /// </summary>
    Task<AuthResponseDto?> LoginAsync(string email, string password);

    /// <summary>
    /// Đăng ký tài khoản mới với vai trò mặc định là Customer.
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(string email, string password, string fullName);
}

