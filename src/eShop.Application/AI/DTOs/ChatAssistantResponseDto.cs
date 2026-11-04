using eShop.Application.Catalog.DTOs;

namespace eShop.Application.AI.DTOs;

/// <summary>
/// Kết quả phản hồi từ AI Assistant: Vừa chứa câu trả lời bằng ngôn ngữ tự nhiên,
/// vừa chứa dữ liệu có cấu trúc (Structured Data) để giao diện hiển thị danh sách sản phẩm.
/// </summary>
public record ChatAssistantResponseDto
{
    /// <summary>
    /// Lời hồi đáp / tư vấn bằng ngôn ngữ tự nhiên của AI.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Danh sách các sản phẩm thực tế được AI tra cứu từ database thông qua Function Calling.
    /// </summary>
    public List<ProductDto> SuggestedProducts { get; init; } = new();

    /// <summary>
    /// Danh sách tên các hàm C# Native Plugin mà AI đã tự động quyết định gọi để thu thập dữ liệu.
    /// </summary>
    public List<string> ToolsInvoked { get; init; } = new();
}

