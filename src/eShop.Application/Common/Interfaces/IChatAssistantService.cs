using eShop.Application.AI.DTOs;

namespace eShop.Application.Common.Interfaces;

/// <summary>
/// Giao diện trừu tượng hóa Trợ lý AI (Clean Architecture & DIP).
/// Tầng Application chỉ cần gọi ChatAsync mà không cần phụ thuộc vào
/// nhà cung cấp LLM cụ thể (OpenAI, Azure OpenAI, Ollama, hay Semantic Kernel).
/// </summary>
public interface IChatAssistantService
{
    Task<ChatAssistantResponseDto> ChatAsync(
        string userMessage, 
        List<ChatMessageDto>? conversationHistory = null, 
        CancellationToken cancellationToken = default);
}

