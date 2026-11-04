using eShop.Application.AI.Commands.ChatWithAssistant;
using eShop.Application.AI.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eShop.API.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .WithTags("AI Shopping Assistant")
            .WithOpenApi();

        // 1. Gửi tin nhắn trò chuyện với AI Shopping Assistant
        group.MapPost("/chat", async (
            [FromBody] ChatWithAssistantCommand command, 
            ISender sender, 
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ChatWithAiAssistant")
        .WithSummary("Trò chuyện và nhận tư vấn mua sắm từ AI Assistant (Semantic Kernel Function Calling)")
        .Produces<ChatAssistantResponseDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem();

        return app;
    }
}

