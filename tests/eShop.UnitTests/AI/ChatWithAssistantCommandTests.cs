using eShop.Application.AI.Commands.ChatWithAssistant;
using eShop.Application.AI.DTOs;
using eShop.Application.Catalog.DTOs;
using eShop.Application.Common.Interfaces;
using Moq;
using Xunit;

namespace eShop.UnitTests.AI;

public class ChatWithAssistantCommandTests
{
    private readonly Mock<IChatAssistantService> _chatServiceMock = new();

    [Fact]
    public void Validator_ShouldHaveError_WhenMessageIsEmpty()
    {
        // Arrange
        var validator = new ChatWithAssistantCommandValidator();
        var command = new ChatWithAssistantCommand { Message = "" };

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Message");
    }

    [Fact]
    public void Validator_ShouldPass_WhenMessageIsValid()
    {
        // Arrange
        var validator = new ChatWithAssistantCommandValidator();
        var command = new ChatWithAssistantCommand { Message = "Tôi muốn tìm laptop Apple" };

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Handler_ShouldCallChatAssistantService_AndReturnResponse()
    {
        // Arrange
        var expectedResponse = new ChatAssistantResponseDto
        {
            Message = "Dạ chào bạn, tôi tìm thấy iPhone 15 Pro Max phù hợp với yêu cầu của bạn.",
            SuggestedProducts = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "iPhone 15 Pro Max", Price = 29990000m }
            },
            ToolsInvoked = new List<string> { "SearchProductsAsync" }
        };

        _chatServiceMock
            .Setup(s => s.ChatAsync(
                "Tìm điện thoại Apple dưới 30 triệu", 
                null, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var handler = new ChatWithAssistantCommandHandler(_chatServiceMock.Object);
        var command = new ChatWithAssistantCommand { Message = "Tìm điện thoại Apple dưới 30 triệu" };

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(expectedResponse.Message, response.Message);
        Assert.Single(response.SuggestedProducts);
        Assert.Equal("iPhone 15 Pro Max", response.SuggestedProducts[0].Name);
        Assert.Contains("SearchProductsAsync", response.ToolsInvoked);

        _chatServiceMock.Verify(s => s.ChatAsync(
            "Tìm điện thoại Apple dưới 30 triệu", 
            null, 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

