using eShop.Application.Catalog.DTOs;

namespace eShop.Infrastructure.AI;

/// <summary>
/// Scoped Context lưu trữ trạng thái của phiên gọi AI:
/// Ghi nhận các Tool C# đã được gọi và danh sách ProductDto thực tế để trả về cho Frontend.
/// </summary>
public class AiChatTracker
{
    public List<string> ToolsInvoked { get; } = new();
    public List<ProductDto> SuggestedProducts { get; } = new();

    public void RecordToolInvocation(string toolName)
    {
        if (!ToolsInvoked.Contains(toolName))
        {
            ToolsInvoked.Add(toolName);
        }
    }

    public void AddSuggestedProducts(IEnumerable<ProductDto> products)
    {
        foreach (var product in products)
        {
            if (!SuggestedProducts.Any(p => p.Id == product.Id))
            {
                SuggestedProducts.Add(product);
            }
        }
    }
}

