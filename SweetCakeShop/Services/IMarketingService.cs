using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IMarketingService
    {
        /// <summary>Lấy danh sách tin tức mới nhất đã được publish.</summary>
        Task<List<News>> GetLatestNewsAsync(int count = 3);

        /// <summary>Lấy danh sách khuyến mãi đang còn hiệu lực.</summary>
        Task<List<Promotion>> GetActivePromotionsAsync();

        /// <summary>Lấy sản phẩm nổi bật để hiển thị trên trang About.</summary>
        Task<List<Product>> GetFeaturedProductsAsync(int count = 8);

        /// <summary>Đăng ký email nhận bản tin. Trả về true nếu đăng ký thành công, false nếu email đã tồn tại.</summary>
        Task<bool> SubscribeNewsletterAsync(string email);

        /// <summary>Kiểm tra xem email đã đăng ký chưa.</summary>
        Task<bool> IsEmailSubscribedAsync(string email);
    }
}
