using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using eShop.Application.Common.Exceptions;
using eShop.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace eShop.Infrastructure.Identity;

/// <summary>
/// Quản lý người dùng và cấp phát JWT Token theo chuẩn Clean Architecture.
/// Cung cấp tài khoản mẫu sẵn có:
/// 1. Admin: admin@eshop.com / Admin@123 (Role: Admin)
/// 2. User:  user@eshop.com  / User@123  (Role: Customer)
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private static readonly ConcurrentDictionary<string, UserRecord> _users = new(StringComparer.OrdinalIgnoreCase);

    public record UserRecord(string Id, string Email, string PasswordHash, string FullName, string Role);

    static AuthService()
    {
        // Khởi tạo 2 tài khoản mẫu hệ thống
        var admin = new UserRecord(
            Id: Guid.NewGuid().ToString(),
            Email: "admin@eshop.com",
            PasswordHash: HashPassword("Admin@123"),
            FullName: "Hệ Thống Quản Trị eShop",
            Role: "Admin"
        );

        var customer = new UserRecord(
            Id: Guid.NewGuid().ToString(),
            Email: "user@eshop.com",
            PasswordHash: HashPassword("User@123"),
            FullName: "Nguyễn Văn A (Khách Hàng Mẫu)",
            Role: "Customer"
        );

        _users[admin.Email] = admin;
        _users[customer.Email] = customer;
    }

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<AuthResponseDto?> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult<AuthResponseDto?>(null);

        if (!_users.TryGetValue(email.Trim(), out var user))
            return Task.FromResult<AuthResponseDto?>(null);

        if (user.PasswordHash != HashPassword(password))
            return Task.FromResult<AuthResponseDto?>(null);

        var token = GenerateToken(user, out var expiresInSeconds);

        return Task.FromResult<AuthResponseDto?>(new AuthResponseDto(
            Id: user.Id,
            Token: token,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            ExpiresInSeconds: expiresInSeconds
        ));
    }

    public Task<AuthResponseDto> RegisterAsync(string email, string password, string fullName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "Email không được để trống.") });

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Password", "Mật khẩu phải từ 6 ký tự trở lên.") });

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (_users.ContainsKey(normalizedEmail))
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "Email này đã được đăng ký.") });

        var newUser = new UserRecord(
            Id: Guid.NewGuid().ToString(),
            Email: normalizedEmail,
            PasswordHash: HashPassword(password),
            FullName: string.IsNullOrWhiteSpace(fullName) ? normalizedEmail : fullName.Trim(),
            Role: "Customer"
        );

        _users[normalizedEmail] = newUser;

        var token = GenerateToken(newUser, out var expiresInSeconds);

        return Task.FromResult(new AuthResponseDto(
            Id: newUser.Id,
            Token: token,
            Email: newUser.Email,
            FullName: newUser.FullName,
            Role: newUser.Role,
            ExpiresInSeconds: expiresInSeconds
        ));
    }

    private string GenerateToken(UserRecord user, out int expiresInSeconds)
    {
        var secret = _configuration["JwtSettings:Secret"] 
            ?? "eShop_CleanArchitecture_Super_Secret_Key_For_JWT_Authentication_2026_DotNet8!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "eShop.API";
        var audience = _configuration["JwtSettings:Audience"] ?? "eShop.Client";
        var expiryMinutes = int.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var exp) ? exp : 120;

        expiresInSeconds = expiryMinutes * 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

