namespace SweetCakeShop.Services.Chat
{
    public interface IChatIdentityService
    {
        string EnsureChatTokenCookie();
        (string? UserId, string? ChatToken) GetIdentity();
        string GetSessionKey();
        void ClearChatTokenCookie();
    }
}
