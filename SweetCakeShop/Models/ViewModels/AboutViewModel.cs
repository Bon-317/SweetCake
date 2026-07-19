using System.ComponentModel.DataAnnotations;
using SweetCakeShop.Models;

namespace SweetCakeShop.Models.ViewModels
{
    /// <summary>
    /// ViewModel tổng hợp cho trang Giới thiệu (About) – Marketing module.
    /// </summary>
    public class AboutViewModel
    {
        public List<Product>   FeaturedProducts { get; set; } = new();
        public List<News>      LatestNews       { get; set; } = new();
        public List<Promotion> ActivePromotions { get; set; } = new();

        /// <summary>Form đăng ký newsletter được nhúng vào trang About.</summary>
        public NewsletterSubscribeViewModel NewsletterForm { get; set; } = new();
    }

    /// <summary>
    /// Form đăng ký nhận bản tin email.
    /// </summary>
    public class NewsletterSubscribeViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(256, ErrorMessage = "Email quá dài.")]
        [Display(Name = "Email của bạn")]
        public string Email { get; set; } = string.Empty;
    }
}
