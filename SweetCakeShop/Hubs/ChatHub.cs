using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SweetCakeShop.Hubs
{
    public class ChatHub : Hub
    {
        // Khách hàng hoặc Admin tham gia vào phòng chat riêng của từng phiên (theo SessionKey)
        public async Task JoinChatSession(string sessionKey)
        {
            if (!string.IsNullOrEmpty(sessionKey))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionKey);
            }
        }

        public async Task LeaveChatSession(string sessionKey)
        {
            if (!string.IsNullOrEmpty(sessionKey))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionKey);
            }
        }

        // Admin tham gia vào phòng chung dành cho Admin để nhận tin nhắn real-time từ tất cả khách hàng
        public async Task JoinAdminRoom()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminRoom");
        }

        public async Task LeaveAdminRoom()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminRoom");
        }
    }
}
