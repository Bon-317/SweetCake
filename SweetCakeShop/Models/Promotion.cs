using System.ComponentModel.DataAnnotations;

namespace SweetCakeShop.Models
{
    /// <summary>
    /// Chuong trinh khuyen mai / uu dai cua SweetCake.
    /// </summary>
    public class Promotion
    {
        public int PromotionId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Phan tram giam gia (0-100). null = giam gia co dinh hoac qua tang.</summary>
        public int? DiscountPercent { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Nhan hien thi tren badge, vi du: "HOT", "MOI", "SAP HET HAN".</summary>
        [MaxLength(30)]
        public string? BadgeText { get; set; }
    }
}
