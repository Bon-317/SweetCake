using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.Api;

namespace SweetCakeShop.Services.Chat
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IChatIdentityService _identity;

        public ChatHistoryService(ApplicationDbContext db, IChatIdentityService identity)
        {
            _db = db;
            _identity = identity;
        }

        private IQueryable<CustomerChatMessage> Query()
        {
            var (userId, token) = _identity.GetIdentity();
            if (!string.IsNullOrEmpty(userId))
                return _db.CustomerChatMessages.Where(m => m.UserId == userId);
            if (!string.IsNullOrEmpty(token))
                return _db.CustomerChatMessages.Where(m => m.ChatToken == token);
            return _db.CustomerChatMessages.Where(m => false);
        }

        public Task<List<CustomerChatMessage>> GetRecentAsync(int take = 6, CancellationToken ct = default) =>
            Query().OrderByDescending(m => m.CreatedAt).Take(take)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(ct);

        public async Task<List<ChatMessageDto>> GetHistoryForUiAsync(CancellationToken ct = default)
        {
            var rows = await Query().OrderBy(m => m.CreatedAt).Take(50).ToListAsync(ct);
            return rows.Select(m => new ChatMessageDto
            {
                Sender = m.Sender,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        public async Task<CustomerChatMessage> AddUserMessageAsync(
            string content, int? contextProductId, CancellationToken ct = default)
        {
            var (userId, token) = _identity.GetIdentity();
            if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(token))
                token = _identity.EnsureChatTokenCookie();

            var msg = new CustomerChatMessage
            {
                UserId = userId,
                ChatToken = userId == null ? token : null,
                Sender = "user",
                Content = content.Trim(),
                ContextProductId = contextProductId,
                CreatedAt = DateTime.UtcNow
            };
            _db.CustomerChatMessages.Add(msg);
            await _db.SaveChangesAsync(ct);
            return msg;
        }

        public async Task<CustomerChatMessage> AddModelMessageAsync(string content, CancellationToken ct = default)
        {
            var (userId, token) = _identity.GetIdentity();
            var msg = new CustomerChatMessage
            {
                UserId = userId,
                ChatToken = userId == null ? token : null,
                Sender = "model",
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.CustomerChatMessages.Add(msg);
            await _db.SaveChangesAsync(ct);
            return msg;
        }

        public Task<bool> HasAnyMessagesAsync(CancellationToken ct = default) =>
            Query().AnyAsync(ct);

        public async Task MergeChatTokenToUserAsync(string chatToken, string userId, CancellationToken ct = default)
        {
            var rows = await _db.CustomerChatMessages
                .Where(m => m.ChatToken == chatToken)
                .ToListAsync(ct);
            foreach (var m in rows)
            {
                m.UserId = userId;
                m.ChatToken = null;
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<object>> ListActiveSessionsForAdminAsync(CancellationToken ct = default)
        {
            var all = await _db.CustomerChatMessages
                .OrderByDescending(m => m.CreatedAt)
                .Take(500)
                .ToListAsync(ct);

            var groups = all
                .Where(m => !string.IsNullOrEmpty(m.UserId) || !string.IsNullOrEmpty(m.ChatToken))
                .GroupBy(m => !string.IsNullOrEmpty(m.UserId) ? "usr_" + m.UserId : "tok_" + m.ChatToken)
                .Select(g =>
                {
                    var last = g.First();
                    return (object)new
                    {
                        sessionKey = g.Key,
                        lastMessage = last.Content,
                        lastSender = last.Sender,
                        lastUpdated = last.CreatedAt.AddHours(7).ToString("HH:mm dd/MM/yyyy"),
                        count = g.Count()
                    };
                })
                .ToList();

            return groups;
        }

        public async Task<List<ChatMessageDto>> GetHistoryBySessionKeyAsync(string sessionKey, CancellationToken ct = default)
        {
            IQueryable<CustomerChatMessage> query;
            if (sessionKey.StartsWith("usr_"))
            {
                var uid = sessionKey.Substring(4);
                query = _db.CustomerChatMessages.Where(m => m.UserId == uid);
            }
            else if (sessionKey.StartsWith("tok_"))
            {
                var tok = sessionKey.Substring(4);
                query = _db.CustomerChatMessages.Where(m => m.ChatToken == tok);
            }
            else
            {
                return new List<ChatMessageDto>();
            }

            var rows = await query.OrderBy(m => m.CreatedAt).Take(100).ToListAsync(ct);
            return rows.Select(m => new ChatMessageDto
            {
                Sender = m.Sender,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        public async Task<CustomerChatMessage> AddAdminMessageAsync(string sessionKey, string content, CancellationToken ct = default)
        {
            string? userId = null;
            string? chatToken = null;
            if (sessionKey.StartsWith("usr_")) userId = sessionKey.Substring(4);
            else if (sessionKey.StartsWith("tok_")) chatToken = sessionKey.Substring(4);

            var msg = new CustomerChatMessage
            {
                UserId = userId,
                ChatToken = chatToken,
                Sender = "Admin",
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.CustomerChatMessages.Add(msg);
            await _db.SaveChangesAsync(ct);
            return msg;
        }
    }
}
