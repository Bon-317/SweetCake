using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services.AI.Rag;

namespace SweetCakeShop.Services.AI.Customer
{
    public sealed class CustomerAssistantReply
    {
        public string Reply { get; init; } = string.Empty;
        public IReadOnlyList<ProductFactDto> Products { get; init; } = [];
    }

    public interface IIntelligentCustomerChatService
    {
        Task<CustomerAssistantReply?> TryGenerateReplyAsync(
            string userMessage,
            IReadOnlyList<CustomerChatMessage> history,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Native OpenAI function-calling orchestration for the public customer chat. It exposes
    /// read-only application tools (products, news, promotions, orders) and returns null to let the legacy safe fallback handle
    /// requests when an AI provider is unavailable.
    /// </summary>
    public sealed class SemanticKernelCustomerAssistant : IIntelligentCustomerChatService
    {
        private const string SystemPrompt = """
            Bạn là nhân viên tư vấn SweetCakeShop, trả lời bằng tiếng Việt thân thiện và ngắn gọn.
            Với câu hỏi về bánh, giá, chi tiết, bánh tương tự, gợi ý, giỏ hàng, chương trình khuyến mãi, tin tức hoặc tra cứu trạng thái đơn hàng, phải gọi function phù hợp để lấy dữ liệu thật từ hệ thống trước khi trả lời.
            Không tự bịa tên bánh, giá, khuyến mãi, tồn kho, lịch giao hoặc trạng thái đơn. Nếu function không có dữ liệu, nói rõ cửa hàng chưa có thông tin đó.
            Chỉ tư vấn và hướng dẫn; không tự xác nhận đơn, không tự thay đổi giỏ hàng hay yêu cầu thông tin nhạy cảm.
            Trả lời tối đa ba câu, xưng em và gọi khách là anh/chị.
            """;

        private readonly IConfiguration _configuration;
        private readonly ILogger<SemanticKernelCustomerAssistant> _logger;
        private readonly IProductAnalyticsService _products;
        private readonly IRecommendationService _recommendations;
        private readonly CartService _cart;
        private readonly ICustomerToolCallContext _toolContext;
        private readonly ApplicationDbContext _context;
        private readonly IHybridRagSearchService _hybridSearch;

        public SemanticKernelCustomerAssistant(
            IConfiguration configuration,
            ILogger<SemanticKernelCustomerAssistant> logger,
            IProductAnalyticsService products,
            IRecommendationService recommendations,
            CartService cart,
            ICustomerToolCallContext toolContext,
            ApplicationDbContext context,
            IHybridRagSearchService hybridSearch)
        {
            _configuration = configuration;
            _logger = logger;
            _products = products;
            _recommendations = recommendations;
            _cart = cart;
            _toolContext = toolContext;
            _context = context;
            _hybridSearch = hybridSearch;
        }

        public async Task<CustomerAssistantReply?> TryGenerateReplyAsync(
            string userMessage,
            IReadOnlyList<CustomerChatMessage> history,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            try
            {
                _toolContext.Clear();

                var builder = Kernel.CreateBuilder();
                builder.AddOpenAIChatCompletion(
                    _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
                    apiKey.Trim());
                builder.Plugins.AddFromObject(
                    new BakeryCustomerPlugin(_products, _recommendations, _cart, _toolContext, _context, _hybridSearch),
                    "Bakery");

                var kernel = builder.Build();
                var chat = kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory(SystemPrompt);

                foreach (var message in history.TakeLast(6))
                {
                    if (message.Sender.Equals("model", StringComparison.OrdinalIgnoreCase))
                        chatHistory.AddAssistantMessage(message.Content);
                    else
                        chatHistory.AddUserMessage(message.Content);
                }

                chatHistory.AddUserMessage(userMessage);

                var settings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.15f,
                    MaxTokens = 280,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                var answer = await chat.GetChatMessageContentAsync(
                    chatHistory, settings, kernel, cancellationToken);
                var reply = answer.Content?.Trim();

                return string.IsNullOrWhiteSpace(reply)
                    ? null
                    : new CustomerAssistantReply
                    {
                        Reply = reply,
                        Products = _toolContext.Products.ToList()
                    };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Semantic Kernel customer assistant failed; using fallback chat flow.");
                return null;
            }
        }
    }
}
