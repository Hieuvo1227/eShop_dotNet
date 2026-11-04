using System.Text.RegularExpressions;
using eShop.Application.AI.DTOs;
using eShop.Application.Common.Interfaces;
using eShop.Infrastructure.AI.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace eShop.Infrastructure.AI.Services;

/// <summary>
/// Triển khai dịch vụ Trợ lý AI mua sắm thông minh (AI Shopping Assistant) bằng Microsoft Semantic Kernel.
/// Tự động kích hoạt Function Calling để tra cứu dữ liệu thời gian thực từ Database.
/// Hỗ trợ chuyển đổi mượt mà giữa OpenAI / Azure OpenAI thật và Chế độ Semantic Fallback cục bộ.
/// </summary>
public class SemanticKernelAssistantService : IChatAssistantService
{
    private readonly ShopCatalogPlugin _catalogPlugin;
    private readonly AiChatTracker _tracker;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SemanticKernelAssistantService> _logger;

    private const string SystemPrompt = 
        "Bạn là 'eShop Assistant' - Trợ lý tư vấn mua sắm trí tuệ nhân tạo chuyên nghiệp của hệ thống eCommerce eShop. " +
        "Bạn giao tiếp thân thiện, ngắn gọn và hữu ích bằng tiếng Việt. " +
        "Nhiệm vụ của bạn là tư vấn thông tin sản phẩm, kiểm tra tồn kho và gợi ý lựa chọn tối ưu theo ngân sách. " +
        "Hãy luôn sử dụng các công cụ C# (Plugins) được cung cấp để tra cứu thông tin chính xác từ Database. " +
        "Không bao giờ bịa đặt giá cả hay cấu hình sản phẩm không có thật trong hệ thống.";

    public SemanticKernelAssistantService(
        ShopCatalogPlugin catalogPlugin,
        AiChatTracker tracker,
        IConfiguration configuration,
        ILogger<SemanticKernelAssistantService> logger)
    {
        _catalogPlugin = catalogPlugin;
        _tracker = tracker;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ChatAssistantResponseDto> ChatAsync(
        string userMessage, 
        List<ChatMessageDto>? conversationHistory = null, 
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["AI:OpenAI:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var modelId = _configuration["AI:OpenAI:ModelId"] ?? "gpt-4o-mini";

        // 1. Nếu có OpenAI API Key hợp lệ -> Dùng Semantic Kernel kết nối LLM thật
        if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("sk-placeholder", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return await ExecuteWithSemanticKernelAsync(apiKey, modelId, userMessage, conversationHistory, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối tới OpenAI API. Đang tự động chuyển sang chế độ Semantic Fallback Engine cục bộ.");
            }
        }

        // 2. Chế độ Fallback Cục bộ (Zero Friction Local Dev & Unit Test)
        // Mô phỏng quá trình AI nhận thức ý định (Intent Recognition) và tự động gọi Function Calling
        return await ExecuteLocalSemanticFallbackAsync(userMessage, conversationHistory, cancellationToken);
    }

    private async Task<ChatAssistantResponseDto> ExecuteWithSemanticKernelAsync(
        string apiKey,
        string modelId,
        string userMessage,
        List<ChatMessageDto>? conversationHistory,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Đang gọi Semantic Kernel với mô hình LLM '{ModelId}'...", modelId);

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
        kernelBuilder.Plugins.AddFromObject(_catalogPlugin, "ShopCatalog");

        var kernel = kernelBuilder.Build();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt);

        if (conversationHistory != null)
        {
            foreach (var msg in conversationHistory)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    history.AddUserMessage(msg.Content);
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    history.AddAssistantMessage(msg.Content);
            }
        }

        history.AddUserMessage(userMessage);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatService.GetChatMessageContentAsync(
            history, 
            settings, 
            kernel, 
            cancellationToken);

        return new ChatAssistantResponseDto
        {
            Message = response.Content ?? "Dạ, tôi có thể giúp gì thêm cho bạn về các sản phẩm của eShop?",
            SuggestedProducts = _tracker.SuggestedProducts.ToList(),
            ToolsInvoked = _tracker.ToolsInvoked.ToList()
        };
    }

    private async Task<ChatAssistantResponseDto> ExecuteLocalSemanticFallbackAsync(
        string userMessage,
        List<ChatMessageDto>? conversationHistory,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Đang thực thi AI Semantic Fallback Engine cho câu hỏi: '{Message}'", userMessage);

        var lower = userMessage.ToLower();
        string responseText;

        // Trích xuất mức giá tối đa nếu có (vd: "dưới 30 triệu", "duoi 30tr", "dưới 50000000")
        decimal? maxPrice = null;
        var priceMatch = Regex.Match(lower, @"(dưới|duoi|<|nhỏ hơn|nho hon)\s*(\d+([\.,]\d+)?)\s*(triệu|trieu|tr|vnd|đ|k)?");
        if (priceMatch.Success)
        {
            if (decimal.TryParse(priceMatch.Groups[2].Value.Replace(",", "."), out var number))
            {
                var unit = priceMatch.Groups[4].Value;
                if (unit == "triệu" || unit == "trieu" || unit == "tr")
                    maxPrice = number * 1_000_000m;
                else if (unit == "k")
                    maxPrice = number * 1_000m;
                else if (number < 1000)
                    maxPrice = number * 1_000_000m;
                else
                    maxPrice = number;
            }
        }

        // Nhận diện thương hiệu (Brand)
        string? brandName = null;
        if (lower.Contains("apple") || lower.Contains("iphone") || lower.Contains("macbook") || lower.Contains("ipad"))
            brandName = "Apple";
        else if (lower.Contains("samsung") || lower.Contains("galaxy"))
            brandName = "Samsung";
        else if (lower.Contains("logitech"))
            brandName = "Logitech";
        else if (lower.Contains("sony"))
            brandName = "Sony";
        else if (lower.Contains("dell"))
            brandName = "Dell";

        // Nhận diện danh mục / ngành hàng (Category)
        string? categoryName = null;
        if (lower.Contains("điện thoại") || lower.Contains("dien thoai") || lower.Contains("smartphone"))
            categoryName = "Smartphones";
        else if (lower.Contains("laptop") || lower.Contains("máy tính xách tay"))
            categoryName = "Laptops";
        else if (lower.Contains("chuột") || lower.Contains("chuot") || lower.Contains("bàn phím") || lower.Contains("ban phim") || lower.Contains("phụ kiện"))
            categoryName = "Phụ kiện";
        else if (lower.Contains("tai nghe") || lower.Contains("âm thanh") || lower.Contains("loa"))
            categoryName = "Âm thanh";

        // Nhận diện từ khóa sản phẩm cụ thể (Specific model)
        string? searchTerm = null;
        if (lower.Contains("iphone")) searchTerm = "iPhone";
        else if (lower.Contains("galaxy")) searchTerm = "Galaxy";
        else if (lower.Contains("macbook")) searchTerm = "MacBook";
        else if (lower.Contains("chuột") || lower.Contains("chuot")) searchTerm = "Chuột";
        else if (lower.Contains("bàn phím") || lower.Contains("ban phim")) searchTerm = "Bàn phím";
        else if (lower.Contains("tai nghe")) searchTerm = "Tai nghe";

        // Quyết định gọi công cụ Function Calling
        if (lower.Contains("tồn kho") || lower.Contains("ton kho") || lower.Contains("còn hàng") || lower.Contains("con hang") || lower.Contains("còn bao nhiêu") || lower.Contains("con bao nhieu"))
        {
            // Kiểm tra kho
            var searchResult = await _catalogPlugin.SearchProductsAsync(searchTerm, maxPrice, brandName, categoryName);
            var firstProduct = _tracker.SuggestedProducts.FirstOrDefault();
            if (firstProduct != null)
            {
                var stockInfo = await _catalogPlugin.CheckProductStockAsync(firstProduct.Id);
                responseText = $"Dạ chào bạn! eShop đã kiểm tra tình trạng kho hàng:\n{stockInfo}\n\nSản phẩm hiện đang có sẵn để phục vụ bạn đặt mua ngay hôm nay!";
            }
            else
            {
                responseText = "Dạ hiện tại eShop chưa tìm thấy sản phẩm này trong kho hàng. Bạn có muốn tham khảo các mẫu tương đương khác không ạ?";
            }
        }
        else if (!string.IsNullOrEmpty(searchTerm) || !string.IsNullOrEmpty(brandName) || !string.IsNullOrEmpty(categoryName) || maxPrice.HasValue || lower.Contains("tìm") || lower.Contains("tim") || lower.Contains("mua") || lower.Contains("tư vấn") || lower.Contains("tu van"))
        {
            // Tìm kiếm sản phẩm
            var searchSummary = await _catalogPlugin.SearchProductsAsync(searchTerm, maxPrice, brandName, categoryName);
            
            if (_tracker.SuggestedProducts.Any())
            {
                var productNames = string.Join(", ", _tracker.SuggestedProducts.Select(p => $"**{p.Name}** ({p.Price:N0} VND)"));
                responseText = $"Dạ chào bạn! Theo yêu cầu của bạn, eShop Assistant đã tìm thấy các lựa chọn lý tưởng sau:\n\n" +
                               $"{productNames}\n\n" +
                               $"Tất cả sản phẩm trên đều là hàng chính hãng, đang sẵn hàng trong kho. Bạn có thể bấm vào sản phẩm bên dưới để thêm vào giỏ hàng hoặc hỏi tôi thêm về cấu hình chi tiết nhé!";
            }
            else
            {
                responseText = $"Dạ tiếc quá, hiện tại eShop chưa có sản phẩm nào khớp hoàn toàn với tiêu chí{(maxPrice.HasValue ? $" dưới {maxPrice.Value:N0} VND" : "")}. Bạn có muốn mở rộng tầm giá hoặc xem các danh mục khác không ạ?";
            }
        }
        else
        {
            // Chào hỏi chung
            responseText = "Dạ xin chào bạn! Tôi là eShop AI Shopping Assistant. " +
                           "Tôi có thể hỗ trợ bạn tìm kiếm sản phẩm theo sở thích, kiểm tra số lượng tồn kho theo thời gian thực " +
                           "hoặc gợi ý các thiết bị điện thoại, laptop, phụ kiện công nghệ phù hợp với ngân sách của bạn. " +
                           "Bạn đang cần tìm sản phẩm gì hôm nay ạ?";
        }

        return new ChatAssistantResponseDto
        {
            Message = responseText,
            SuggestedProducts = _tracker.SuggestedProducts.ToList(),
            ToolsInvoked = _tracker.ToolsInvoked.ToList()
        };
    }
}
