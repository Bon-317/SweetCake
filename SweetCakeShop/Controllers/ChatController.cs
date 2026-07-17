using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SweetCakeShop.Constants;
using SweetCakeShop.Hubs;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services;
using SweetCakeShop.Services.AI;
using SweetCakeShop.Services.Chat;

namespace SweetCakeShop.Controllers
{
    public class AdminReplyCustomerRequest
    {
        public string SessionKey { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chatbot tư vấn sản phẩm (flow video Laravel + Gemini): DB history, ChatToken, catalog từ SQL + SignalR Live Chat.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ICustomerProductChatService _customerChat;
        private readonly IAiChatService _adminChat;
        private readonly IConversationMemoryService _adminMemory;
        private readonly IChatHistoryService _historyService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(
            ICustomerProductChatService customerChat,
            IAiChatService adminChat,
            IConversationMemoryService adminMemory,
            IChatHistoryService historyService,
            IHubContext<ChatHub> hubContext)
        {
            _customerChat = customerChat;
            _adminChat = adminChat;
            _adminMemory = adminMemory;
            _historyService = historyService;
            _hubContext = hubContext;
        }

        /// <summary>Lịch sử chat — UserId hoặc Cookie ChatToken.</summary>
        [HttpGet("GetChatHistory")]
        [AllowAnonymous]
        public async Task<ActionResult<ChatHistoryResponse>> GetChatHistory(CancellationToken ct) =>
            Ok(await _customerChat.GetChatHistoryAsync(ct));

        /// <summary>Gửi tin nhắn → lưu DB → Gemini/OpenAI + dữ liệu sản phẩm + SignalR Real-time.</summary>
        [HttpPost("SendMessage")]
        [AllowAnonymous]
        public async Task<ActionResult<SendChatMessageResponse>> SendMessage(
            [FromBody] SendChatMessageRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest(new SendChatMessageResponse { Success = false, Reply = "Tin nhắn không hợp lệ." });

            return Ok(await _customerChat.SendMessageAsync(request, ct));
        }

        [HttpGet("customer/suggestions")]
        [AllowAnonymous]
        public ActionResult<object> CustomerSuggestions([FromQuery] int? productId)
        {
            var replies = productId.HasValue
                ? new[] { "Giá món này?", "Bánh tương tự?", "Giao hàng?", "Muốn đặt hàng" }
                : new[] { "Bánh sinh nhật gợi ý?", "Bánh rẻ nhất?", "Giao hàng mấy ngày?", "Muốn đặt hàng" };
            return Ok(new { quickReplies = replies });
        }

        [HttpPost("admin")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public async Task<ActionResult<ChatApiResponse>> Admin(
            [FromBody] ChatApiRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ChatApiResponse { Success = false, Reply = "Tin nhắn không hợp lệ." });

            var reply = await _adminChat.GetAdminReplyAsync(request.Message, cancellationToken);
            return Ok(new ChatApiResponse { Success = true, Reply = reply });
        }

        [HttpPost("admin/clear")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public IActionResult ClearAdminSession()
        {
            _adminMemory.Clear(AiChatMode.Admin);
            return Ok(new { success = true });
        }

        [HttpGet("admin/sessions")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public async Task<ActionResult<object>> AdminGetSessions(CancellationToken ct)
        {
            var sessions = await _historyService.ListActiveSessionsForAdminAsync(ct);
            return Ok(new { success = true, sessions });
        }

        [HttpGet("admin/history")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public async Task<ActionResult<object>> AdminGetHistory([FromQuery] string sessionKey, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
                return BadRequest(new { success = false, message = "Thiếu SessionKey" });

            var messages = await _historyService.GetHistoryBySessionKeyAsync(sessionKey, ct);
            return Ok(new { success = true, messages });
        }

        [HttpPost("admin/reply-customer")]
        [Authorize(Roles = nameof(Roles.Admin))]
        public async Task<ActionResult<object>> AdminReplyCustomer(
            [FromBody] AdminReplyCustomerRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionKey) || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { success = false, message = "Tin nhắn không hợp lệ." });

            var msg = await _historyService.AddAdminMessageAsync(request.SessionKey, request.Message, ct);

            await _hubContext.Clients.Group(request.SessionKey).SendAsync("ReceiveMessage", new
            {
                sender = "Admin",
                text = request.Message,
                timestamp = DateTime.Now.ToString("HH:mm")
            }, ct);

            await _hubContext.Clients.Group("AdminRoom").SendAsync("ReceiveAdminReplyConfirmation", new
            {
                sessionKey = request.SessionKey,
                sender = "Admin",
                text = request.Message,
                timestamp = DateTime.Now.ToString("HH:mm")
            }, ct);

            return Ok(new { success = true });
        }
    }
}
