using eShop.Application.Common.Interfaces;

namespace eShop.Web.Services;

public class UserSession
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Quản lý trạng thái Xác thực & Phân quyền phía Client (Blazor Authentication State).
/// </summary>
public class AuthStateService
{
    private readonly CartStateService _cartState;
    public UserSession? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrWhiteSpace(CurrentUser.Token);
    public bool IsAdmin => IsAuthenticated && CurrentUser?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
    public string CurrentBuyerId => CurrentUser?.Email ?? _cartState.BuyerId;

    public event Action? OnChange;

    public AuthStateService(CartStateService cartState)
    {
        _cartState = cartState;
    }

    public void SetUser(AuthResponseDto auth)
    {
        CurrentUser = new UserSession
        {
            Email = auth.Email,
            FullName = auth.FullName,
            Role = auth.Role,
            Token = auth.Token
        };

        // Đồng bộ BuyerId giỏ hàng sang email người dùng
        _cartState.BuyerId = auth.Email;

        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        _cartState.BuyerId = "buyer-demo";
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

