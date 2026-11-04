namespace eShop.Domain.Common;

/// <summary>
/// Mở rộng BaseEntity để theo dõi lịch sử kiểm toán (Audit Log):
/// Ai tạo, tạo khi nào, ai sửa đổi lần cuối, sửa đổi khi nào.
/// Tự động cập nhật bởi EF Core Interceptor khi SaveChanges().
/// </summary>
public abstract class BaseAuditableEntity<TId> : BaseEntity<TId>
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }
}

public abstract class BaseAuditableEntity : BaseAuditableEntity<int>
{
}

