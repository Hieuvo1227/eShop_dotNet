namespace eShop.Web.Services;

public enum ToastLevel
{
    Success,
    Error,
    Info,
    Warning
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastLevel Level { get; set; } = ToastLevel.Info;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Dịch vụ thông báo Toast phản ứng nhanh trong ứng dụng Blazor Server.
/// </summary>
public class ToastService
{
    private readonly List<ToastMessage> _toasts = new();
    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

    public event Action? OnChange;

    public void ShowSuccess(string message, string title = "Thành công")
    {
        AddToast(title, message, ToastLevel.Success);
    }

    public void ShowError(string message, string title = "Lỗi")
    {
        AddToast(title, message, ToastLevel.Error);
    }

    public void ShowInfo(string message, string title = "Thông báo")
    {
        AddToast(title, message, ToastLevel.Info);
    }

    public void ShowWarning(string message, string title = "Cảnh báo")
    {
        AddToast(title, message, ToastLevel.Warning);
    }

    public void RemoveToast(Guid id)
    {
        _toasts.RemoveAll(t => t.Id == id);
        NotifyStateChanged();
    }

    private void AddToast(string title, string message, ToastLevel level)
    {
        var toast = new ToastMessage
        {
            Title = title,
            Message = message,
            Level = level
        };

        _toasts.Add(toast);
        NotifyStateChanged();

        // Tự động đóng toast sau 4 giây
        _ = Task.Run(async () =>
        {
            await Task.Delay(4000);
            _toasts.Remove(toast);
            NotifyStateChanged();
        });
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
