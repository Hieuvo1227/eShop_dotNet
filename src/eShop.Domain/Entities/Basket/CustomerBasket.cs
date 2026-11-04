namespace eShop.Domain.Entities.Basket;

/// <summary>
/// Thực thể Giỏ hàng của khách hàng (Lưu trữ In-Memory trên Redis).
/// Áp dụng Rich Domain Model với các phương thức nghiệp vụ quản lý món hàng.
/// </summary>
public class CustomerBasket
{
    /// <summary>
    /// Định danh khách hàng (Có thể là UserId nếu đã đăng nhập, hoặc SessionId nếu là khách vãng lai).
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách các món hàng trong giỏ.
    /// </summary>
    public List<BasketItem> Items { get; set; } = new();

    /// <summary>
    /// Tổng tiền tạm tính của toàn bộ giỏ hàng.
    /// </summary>
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

    public CustomerBasket() { }

    public CustomerBasket(string buyerId)
    {
        BuyerId = buyerId;
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng:
    /// Nếu sản phẩm đã tồn tại -> Cộng dồn số lượng.
    /// Nếu chưa tồn tại -> Thêm dòng mới.
    /// </summary>
    public void AddItem(int productId, string productName, decimal unitPrice, int quantity, string? pictureUrl)
    {
        if (quantity <= 0) return;

        var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            Items.Add(new BasketItem
            {
                ProductId = productId,
                ProductName = productName,
                UnitPrice = unitPrice,
                Quantity = quantity,
                PictureUrl = pictureUrl
            });
        }
    }

    /// <summary>
    /// Cập nhật số lượng của một món hàng:
    /// Nếu số lượng <= 0 -> Tự động xóa khỏi giỏ.
    /// </summary>
    public void UpdateQuantity(int productId, int newQuantity)
    {
        if (newQuantity <= 0)
        {
            RemoveItem(productId);
            return;
        }

        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.Quantity = newQuantity;
        }
    }

    /// <summary>
    /// Xóa một món hàng khỏi giỏ.
    /// </summary>
    public void RemoveItem(int productId)
    {
        Items.RemoveAll(i => i.ProductId == productId);
    }

    /// <summary>
    /// Dọn sạch toàn bộ giỏ hàng (Sau khi đặt hàng thành công).
    /// </summary>
    public void Clear()
    {
        Items.Clear();
    }
}

