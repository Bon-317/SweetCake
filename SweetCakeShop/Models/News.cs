using System.ComponentModel.DataAnnotations;

namespace SweetCakeShop.Models
{
    /// <summary>
    /// Bài tin tức / bài viết blog của SweetCake.
    /// </summary>
    public class News
    {
        public int NewsId { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Summary { get; set; }

        public string? Content { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime PublishedAt { get; set; } = DateTime.Now;

        public bool IsPublished { get; set; } = true;

        [MaxLength(100)]
        public string? Author { get; set; }
    }
}
