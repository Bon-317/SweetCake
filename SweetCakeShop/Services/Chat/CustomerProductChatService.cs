using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using SweetCakeShop.Hubs;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services.AI;
using SweetCakeShop.Services.AI.Rag;
using SweetCakeShop.Services.Chat.Gemini;
using SweetCakeShop.Services.Chat.OpenAi;

namespace SweetCakeShop.Services.Chat
{
    public class CustomerProductChatService : ICustomerProductChatService
    {
        private const string WelcomeMessage =
            "Chào anh/chị! 🍰 Em là nhân viên tư vấn SweetCakeShop — hỏi giá bánh, gợi ý món, giao hàng hay đặt hàng, em trả lời theo menu thật của tiệm nhé!";

        private readonly IChatIdentityService _identity;
        private readonly IChatHistoryService _history;
        private readonly IChatTokenMergeService _merge;
        private readonly IProductCatalogForAiService _catalog;
        private readonly IProductIntentResolver _resolver;
        private readonly IGeminiChatApiService _gemini;
        private readonly IOpenAiChatApiService _openAi;
        private readonly ITopicFilterService _topicFilter;
        private readonly IChatSecurityService _security;
        private readonly IHubContext<ChatHub> _hubContext;

        public CustomerProductChatService(
            IChatIdentityService identity,
            IChatHistoryService history,
            IChatTokenMergeService merge,
            IProductCatalogForAiService catalog,
            IProductIntentResolver resolver,
            IGeminiChatApiService gemini,
            IOpenAiChatApiService openAi,
            ITopicFilterService topicFilter,
            IChatSecurityService security,
            IHubContext<ChatHub> hubContext)
        {
            _identity = identity;
            _history = history;
            _merge = merge;
            _catalog = catalog;
            _resolver = resolver;
            _gemini = gemini;
            _openAi = openAi;
            _topicFilter = topicFilter;
            _security = security;
            _hubContext = hubContext;
        }

        public async Task<ChatHistoryResponse> GetChatHistoryAsync(CancellationToken ct = default)
        {
            await _merge.TryMergeOnAuthenticatedRequestAsync(ct);
            _identity.EnsureChatTokenCookie();
            var sessionKey = _identity.GetSessionKey();

            var has = await _history.HasAnyMessagesAsync(ct);
            if (!has)
            {
                return new ChatHistoryResponse
                {
                    SessionKey = sessionKey,
                    Messages =
                    [
                        new ChatMessageDto { Sender = "model", Content = WelcomeMessage, CreatedAt = DateTime.UtcNow }
                    ],
                    QuickReplies = ChatProductCardMapper.DefaultCustomerQuickReplies()
                };
            }

            var messages = await _history.GetHistoryForUiAsync(ct);
            return new ChatHistoryResponse
            {
                SessionKey = sessionKey,
                Messages = messages,
                QuickReplies = ChatProductCardMapper.DefaultCustomerQuickReplies()
            };
        }

        public async Task<SendChatMessageResponse> SendMessageAsync(
            SendChatMessageRequest request, CancellationToken ct = default)
        {
            var text = (request.UserMessage ?? "").Trim();
            var sessionKey = _identity.GetSessionKey();
            if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
                return new SendChatMessageResponse { Success = false, SessionKey = sessionKey, Reply = "Tin nhắn không hợp lệ." };

            await _merge.TryMergeOnAuthenticatedRequestAsync(ct);
            _identity.EnsureChatTokenCookie();
            sessionKey = _identity.GetSessionKey();

            if (_topicFilter.IsClearlyOffTopic(text))
                return new SendChatMessageResponse { Success = true, SessionKey = sessionKey, Reply = _topicFilter.GetRejectionMessage("vi") };

            if (_security.IsRestrictedRequest(text))
                return new SendChatMessageResponse { Success = true, SessionKey = sessionKey, Reply = _security.GetStaffRejectionMessage("vi", AiChatMode.Customer) };

            await _history.AddUserMessageAsync(text, request.ProductId, ct);

            await _hubContext.Clients.Group("AdminRoom").SendAsync("ReceiveCustomerMessage", new
            {
                sessionKey = sessionKey,
                sender = "user",
                text = text,
                timestamp = DateTime.Now.ToString("HH:mm")
            }, ct);

            var recent = await _history.GetRecentAsync(6, ct);

            var resolved = await _resolver.ResolveAsync(text, request.ProductId, recent, ct);
            string reply;
            var products = ChatProductCardMapper.ToCards(resolved.Products);

            if (resolved.UseDirectReply)
            {
                reply = TrimSentences(resolved.DirectReply, 3);
            }
            else
            {
                var catalog = await _catalog.BuildCatalogTextAsync(ct);
                var system = BuildSystemPrompt(catalog);
                reply = await _gemini.GenerateReplyAsync(system, recent, text, resolved.FactsBlock, ct)
                          ?? await _openAi.GenerateReplyAsync(system, recent, text, resolved.FactsBlock, ct)
                          ?? "Dạ, em chưa kết nối được AI — anh/chị gọi **1900-SWEET** hoặc xem Menu trên web nhé ạ!";
                reply = TrimSentences(reply, 3);
            }

            await _history.AddModelMessageAsync(reply, ct);

            await _hubContext.Clients.Group("AdminRoom").SendAsync("ReceiveCustomerMessage", new
            {
                sessionKey = sessionKey,
                sender = "model",
                text = reply,
                timestamp = DateTime.Now.ToString("HH:mm")
            }, ct);

            await _hubContext.Clients.Group(sessionKey).SendAsync("ReceiveMessage", new
            {
                sender = "model",
                text = reply,
                products = products,
                timestamp = DateTime.Now.ToString("HH:mm")
            }, ct);

            return new SendChatMessageResponse
            {
                Success = true,
                SessionKey = sessionKey,
                Reply = reply,
                Products = products,
                QuickReplies = request.ProductId.HasValue
                    ? ["Giá món này?", "Bánh tương tự?", "Giao hàng?", "Đặt hàng"]
                    : ChatProductCardMapper.DefaultCustomerQuickReplies()
            };
        }

        private static string BuildSystemPrompt(string catalog) => $"""
            Bạn là nhân viên tư vấn bán bánh SweetCakeShop — thân thiện, ngắn gọn (tối đa 2 câu).
            CHỈ trả lời theo DANH MỤC và DỮ LIỆU CÂU HỎI bên dưới. Không bịa bánh/giá.
            Không có trong danh mục: "Dạ hiện tiệm em chưa có loại bánh này, anh/chị đợi em báo nhân viên tiệm hỗ trợ mình ngay nhé ạ!"
            Dùng icon 🍰 🎂 phù hợp. Xưng: Dạ, anh/chị, em.

            {catalog}
            """;

        private static string TrimSentences(string text, int max)
        {
            var cleaned = text.Trim();
            var parts = Regex.Split(cleaned, @"(?<=[.!?…])\s+");
            return parts.Length <= max ? cleaned : string.Join(" ", parts.Take(max));
        }
    }
}
