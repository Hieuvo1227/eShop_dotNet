using System.Net.Http.Headers;

namespace eShop.Web.Services;

/// <summary>
/// DelegatingHandler tự động đính kèm JWT Bearer Token từ AuthStateService (Scoped per Blazor Circuit)
/// vào mọi request gửi từ ShopApiClient tới eShop.API.
/// Đồng thời nếu API phản hồi 401 Unauthorized, handler tự động xóa session và thông báo đăng xuất an toàn.
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly AuthStateService _authState;

    public AuthenticationDelegatingHandler(AuthStateService authState)
    {
        _authState = authState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_authState.IsAuthenticated && !string.IsNullOrWhiteSpace(_authState.CurrentUser?.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authState.CurrentUser.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _authState.Logout();
        }

        return response;
    }
}

