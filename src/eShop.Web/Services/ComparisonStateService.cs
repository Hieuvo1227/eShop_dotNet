using eShop.Application.Catalog.DTOs;

namespace eShop.Web.Services;

/// <summary>
/// Quản lý trạng thái so sánh sản phẩm công nghệ (Product Comparison).
/// Cho phép người dùng chọn tối đa 3 sản phẩm để đối chiếu thông số kỹ thuật.
/// </summary>
public class ComparisonStateService
{
    private readonly List<ProductDto> _comparisonProducts = new();
    public IReadOnlyList<ProductDto> ComparisonProducts => _comparisonProducts.AsReadOnly();

    public int Count => _comparisonProducts.Count;

    public event Action? OnChange;

    public bool IsInComparison(int productId)
    {
        return _comparisonProducts.Any(p => p.Id == productId);
    }

    public bool AddProduct(ProductDto product, out string message)
    {
        if (_comparisonProducts.Any(p => p.Id == product.Id))
        {
            message = $"Sản phẩm '{product.Name}' đã có trong danh sách so sánh.";
            return false;
        }

        if (_comparisonProducts.Count >= 3)
        {
            message = "Bạn chỉ có thể so sánh tối đa 3 sản phẩm cùng lúc. Hãy xóa bớt một sản phẩm trước.";
            return false;
        }

        _comparisonProducts.Add(product);
        message = $"Đã thêm '{product.Name}' vào danh sách so sánh ({_comparisonProducts.Count}/3).";
        NotifyStateChanged();
        return true;
    }

    public void RemoveProduct(int productId)
    {
        var existing = _comparisonProducts.FirstOrDefault(p => p.Id == productId);
        if (existing != null)
        {
            _comparisonProducts.Remove(existing);
            NotifyStateChanged();
        }
    }

    public void Clear()
    {
        _comparisonProducts.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

