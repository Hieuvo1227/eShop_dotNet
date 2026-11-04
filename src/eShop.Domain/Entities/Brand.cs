using eShop.Domain.Common;

namespace eShop.Domain.Entities;

/// <summary>
/// Thương hiệu sản phẩm (Hãng sản xuất: Apple, Samsung, Sony,...).
/// Kế thừa BaseAuditableEntity để lưu lại lịch sử tạo và sửa đổi.
/// </summary>
public class Brand : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Mối quan hệ 1 - N: Một thương hiệu có nhiều sản phẩm
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

