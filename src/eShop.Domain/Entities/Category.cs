using eShop.Domain.Common;

namespace eShop.Domain.Entities;

/// <summary>
/// Ngành hàng / Danh mục sản phẩm (Smartphones, Laptops, Phụ kiện,...).
/// </summary>
public class Category : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Mối quan hệ 1 - N: Một danh mục có nhiều sản phẩm
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

