using SweetCakeShop.Models;
using SweetCakeShop.Models.Api;

namespace SweetCakeShop.Services.Chat
{
    public interface IChatHistoryService
    {
        Task<List<CustomerChatMessage>> GetRecentAsync(int take = 6, CancellationToken ct = default);
        Task<List<ChatMessageDto>> GetHistoryForUiAsync(CancellationToken ct = default);
        Task<CustomerChatMessage> AddUserMessageAsync(string content, int? contextProductId, CancellationToken ct = default);
        Task<CustomerChatMessage> AddModelMessageAsync(string content, CancellationToken ct = default);
        Task<bool> HasAnyMessagesAsync(CancellationToken ct = default);
        Task MergeChatTokenToUserAsync(string chatToken, string userId, CancellationToken ct = default);
        Task<List<object>> ListActiveSessionsForAdminAsync(CancellationToken ct = default);
        Task<List<ChatMessageDto>> GetHistoryBySessionKeyAsync(string sessionKey, CancellationToken ct = default);
        Task<CustomerChatMessage> AddAdminMessageAsync(string sessionKey, string content, CancellationToken ct = default);
        Task<bool> IsUnderAdminAssistanceAsync(string sessionKey, CancellationToken ct = default);
        Task SetAdminAssistanceAsync(string sessionKey, bool active, CancellationToken ct = default);
    }
}
