namespace eShop.Application.AI.DTOs;

/// <summary>
/// Đại diện cho một câu thoại trong lịch sử hội thoại (Chat History).
/// Role có thể là "user", "assistant", hoặc "system".
/// </summary>
public record ChatMessageDto(string Role, string Content);

