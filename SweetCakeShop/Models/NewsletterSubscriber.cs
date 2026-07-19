using System.ComponentModel.DataAnnotations;

namespace SweetCakeShop.Models
{
    /// <summary>
    /// Người đăng ký nhận bản tin (newsletter) của SweetCake.
    /// </summary>
    public class NewsletterSubscriber
    {
        public int SubscriberId { get; set; }

        [Required, MaxLength(256), EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime SubscribedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
