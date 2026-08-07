using SweetCakeShop.Helpers;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services.AI;
using SweetCakeShop.Services.AI.Rag;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// RAG + Gemini/OpenAI — giống flow video [#87] Laravel: DB sản phẩm → context → API → trả lời ngắn.
    /// </summary>
    public class AiChatService : IAiChatService
    {
        private readonly IQueryPlannerService _queryPlanner;
        private readonly IAiFunctionExecutorService _functionExecutor;
        private readonly IRagRetrieverService _ragRetriever;
        private readonly IConsultantResponseService _consultant;
        private readonly IConversationMemoryService _memory;
        private readonly ITopicFilterService _topicFilter;
        private readonly IChatSecurityService _security;
        private readonly IStructuredAiResponseService _structuredResponse;
        private readonly IOrderHandoffService _orderHandoff;
        private readonly IStoreKnowledgeService _storeKnowledge;
        private readonly IChatPageContextService _pageContext;
        private readonly IChatEnrichmentService _enrichment;

        public AiChatService(
            IQueryPlannerService queryPlanner,
            IAiFunctionExecutorService functionExecutor,
            IRagRetrieverService ragRetriever,
            IConsultantResponseService consultant,
            IConversationMemoryService memory,
            ITopicFilterService topicFilter,
            IChatSecurityService security,
            IStructuredAiResponseService structuredResponse,
            IOrderHandoffService orderHandoff,
            IStoreKnowledgeService storeKnowledge,
            IChatPageContextService pageContext,
            IChatEnrichmentService enrichment)
        {
            _queryPlanner = queryPlanner;
            _functionExecutor = functionExecutor;
            _ragRetriever = ragRetriever;
            _consultant = consultant;
            _memory = memory;
            _topicFilter = topicFilter;
            _security = security;
            _structuredResponse = structuredResponse;
            _orderHandoff = orderHandoff;
            _storeKnowledge = storeKnowledge;
            _pageContext = pageContext;
            _enrichment = enrichment;
        }

        public Task<string> GetAdminReplyAsync(string userMessage, CancellationToken cancellationToken = default) =>
            ProcessAdminAsync(userMessage, cancellationToken);

        public Task<ChatReplyResult> GetCustomerReplyAsync(
            string userMessage,
            ChatCustomerContext? clientContext = null,
            CancellationToken cancellationToken = default) =>
            ProcessCustomerAsync(userMessage, clientContext, cancellationToken);

        private async Task<string> ProcessAdminAsync(string userMessage, CancellationToken ct)
        {
            var result = await ProcessCoreAsync(AiChatMode.Admin, userMessage, null, ct);
            return result.Reply;
        }

        private async Task<ChatReplyResult> ProcessCustomerAsync(
            string userMessage,
            ChatCustomerContext? clientContext,
            CancellationToken ct)
        {
            var result = await ProcessCoreAsync(AiChatMode.Customer, userMessage, clientContext, ct);
            result.QuickReplies = BuildQuickReplies(clientContext);
            return result;
        }

        private async Task<ChatReplyResult> ProcessCoreAsync(
            AiChatMode mode,
            string userMessage,
            ChatCustomerContext? clientContext,
            CancellationToken cancellationToken)
        {
            var trimmed = userMessage.Trim();
            var language = ChatLanguageDetector.Detect(trimmed);

            if (string.IsNullOrWhiteSpace(trimmed))
                return new ChatReplyResult { Reply = "Bạn vui lòng nhập câu hỏi nhé." };

            // BƯỚC 1: LỌC TỪ KHÓA BẰNG CODE C# (Không tốn tiền gọi AI)
            // Kiểm tra xem khách có đang hỏi lạc đề không (ví dụ: hỏi sửa xe máy).
            if (_topicFilter.IsClearlyOffTopic(trimmed))
                return new ChatReplyResult { Reply = _topicFilter.GetRejectionMessage(language) };

            // Cảnh vệ: Kiểm tra xem có phải câu hỏi nhạy cảm, cố tình hack hệ thống không.
            if (_security.IsRestrictedRequest(trimmed))
                return new ChatReplyResult { Reply = _security.GetStaffRejectionMessage(language, mode) };

            // Tiếp tân: Kiểm tra xem khách có đang yêu cầu chat với nhân viên thật không.
            if (mode == AiChatMode.Customer && _orderHandoff.RequestsHumanAgent(trimmed))
            {
                var handoffReply = _orderHandoff.GetImmediateHandoffReply(language);
                _memory.AddExchange(mode, trimmed, handoffReply);
                return new ChatReplyResult
                {
                    Reply = handoffReply,
                    QuickReplies = ChatProductCardMapper.DefaultCustomerQuickReplies()
                };
            }

            var sessionState = _memory.GetSessionState(mode);
            var history = _memory.GetHistory(mode);

            if (mode == AiChatMode.Customer)
                _orderHandoff.UpdateFromMessage(sessionState, trimmed);

            // BƯỚC 2: GỌI AI LẦN 1 (QUERY PLANNER)
            // Ép AI đọc tin nhắn của khách và trả về 1 chuỗi JSON chứa TÊN HÀM cần thực thi.
            // Ví dụ: khách gõ "bánh đắt nhất", AI sẽ trả về {"function": "GetHighestPriceProduct"}
            var functionCall = await _queryPlanner.PlanSingleFunctionAsync(
                mode, trimmed, history, sessionState, cancellationToken);

            var plan = new AiFunctionPlan { Calls = [functionCall] };

            // BƯỚC 3: THỰC THI (KÉO DỮ LIỆU TỪ SQL SERVER MANG LÊN RAM)
            // C# đọc cái chuỗi JSON ở trên, chọc vào DB để kéo dữ liệu bánh thật ra ngoài.
            var businessContext = await _functionExecutor.ExecuteAsync(
                mode, plan, trimmed, sessionState, language, cancellationToken);

            businessContext.PrimaryExecutedFunction = functionCall.Name;
            businessContext.PageContext = _pageContext.ResolvePageContext(
                clientContext?.PageUrl, clientContext?.ProductId);

            _memory.UpdateFromContext(mode, businessContext, trimmed);
            _memory.SaveSessionState(mode, sessionState);

            // BƯỚC 4: TẠO NGỮ CẢNH (RAG - Retrieval Augmented Generation)
            // Gom dữ liệu SQL vừa móc được ở Bước 3 + chính sách giao hàng tĩnh (StoreKnowledge) lại thành một cục text siêu bự.
            var knowledge = _ragRetriever.BuildDocument(mode, businessContext);
            var enrichment = _enrichment.BuildEnrichmentBlock(mode, sessionState);
            knowledge.StoreDataBlock = _storeKnowledge.GetCoreKnowledgeBlock(mode) + "\n"
                + await _storeKnowledge.GetLiveCatalogOverviewAsync(ct: cancellationToken) + "\n"
                + enrichment + "\n"
                + knowledge.StoreDataBlock;

            if (!string.IsNullOrWhiteSpace(businessContext.PageContext))
                knowledge.StoreDataBlock += "\n" + businessContext.PageContext + "\n";

            if (mode == AiChatMode.Customer)
            {
                var handoff = _orderHandoff.BuildRagSupplement(sessionState);
                if (!string.IsNullOrWhiteSpace(handoff))
                    knowledge.StoreDataBlock += "\n" + handoff;
            }

            // BƯỚC 5: GỌI AI LẦN 2 (CONSULTANT RESPONSE)
            // Quăng cục text khổng lồ vừa gom được ở Bước 4 vào cho AI. 
            // Căn dặn AI: "Mày đóng vai nhân viên tư vấn, đọc đống dữ liệu SQL tao vừa gửi và trả lời khách thật tự nhiên nhé".
            var reply = await _consultant.GenerateAsync(
                mode, trimmed, knowledge, history, language, sessionState, cancellationToken);

            if (string.IsNullOrWhiteSpace(reply))
                reply = _structuredResponse.Compose(mode, businessContext);

            // Lưu đoạn chat này vào bộ nhớ để câu sau AI còn nhớ ngữ cảnh
            _memory.AddExchange(mode, trimmed, reply);

            // Trả về giao diện người dùng
            return new ChatReplyResult
            {
                Reply = reply,
                Products = ChatProductCardMapper.ToCards(businessContext.Products)
            };
        }

        private static List<string> BuildQuickReplies(ChatCustomerContext? ctx)
        {
            if (ctx?.ProductId != null || (ctx?.PageUrl?.Contains("/Details", StringComparison.OrdinalIgnoreCase) ?? false))
                return ["Giá món này?", "Bánh tương tự?", "Giao hàng?", "Thêm vào giỏ"];
            return ChatProductCardMapper.DefaultCustomerQuickReplies();
        }

    }
}
