using eShop.Application.AI.DTOs;
using eShop.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace eShop.Application.AI.Commands.ChatWithAssistant;

/// <summary>
/// Command gửi câu hỏi tới AI Shopping Assistant kèm theo lịch sử hội thoại.
/// </summary>
public record ChatWithAssistantCommand : IRequest<ChatAssistantResponseDto>
{
    public string Message { get; init; } = string.Empty;
    public List<ChatMessageDto>? History { get; init; }
}

public class ChatWithAssistantCommandValidator : AbstractValidator<ChatWithAssistantCommand>
{
    public ChatWithAssistantCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Nội dung tin nhắn gửi tới AI không được để trống.")
            .MaximumLength(1000).WithMessage("Nội dung tin nhắn không được vượt quá 1000 ký tự.");
    }
}

public class ChatWithAssistantCommandHandler : IRequestHandler<ChatWithAssistantCommand, ChatAssistantResponseDto>
{
    private readonly IChatAssistantService _chatAssistantService;

    public ChatWithAssistantCommandHandler(IChatAssistantService chatAssistantService)
    {
        _chatAssistantService = chatAssistantService;
    }

    public async Task<ChatAssistantResponseDto> Handle(ChatWithAssistantCommand request, CancellationToken cancellationToken)
    {
        return await _chatAssistantService.ChatAsync(
            request.Message, 
            request.History, 
            cancellationToken);
    }
}

