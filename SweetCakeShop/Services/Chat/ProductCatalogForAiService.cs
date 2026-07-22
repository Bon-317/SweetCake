using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services.AI.Rag;

namespace SweetCakeShop.Services.Chat
{
    public interface IProductCatalogForAiService
    {
        Task<string> BuildCatalogTextAsync(CancellationToken ct = default);
        Task<string> BuildRelevantCatalogTextAsync(string userQuery, int take = 10, CancellationToken ct = default);
        Task<List<ProductFactDto>> GetAllProductsAsync(CancellationToken ct = default);
    }

    public class ProductCatalogForAiService : IProductCatalogForAiService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHybridRagSearchService _hybridSearch;

        public ProductCatalogForAiService(ApplicationDbContext db, IHybridRagSearchService hybridSearch)
        {
            _db = db;
            _hybridSearch = hybridSearch;
        }

        public async Task<List<ProductFactDto>> GetAllProductsAsync(CancellationToken ct = default) =>
            await (
                from p in _db.Products.AsNoTracking()
                join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                orderby p.Price descending
                select new ProductFactDto
                {
                    ProductId = p.ProductId,
                    Name = p.ProductName,
                    Price = p.Price,
                    Category = c.CategoryName,
                    Description = p.Description,
                    ImageUrl = p.Image
                }).ToListAsync(ct);

        public async Task<string> BuildCatalogTextAsync(CancellationToken ct = default)
        {
            var items = await GetAllProductsAsync(ct);
            var sb = new StringBuilder();
            sb.AppendLine("DANH MỤC BÁNH SWEETCAKESHOP (CHỈ được dùng nguồn này):");
            foreach (var p in items)
            {
                var desc = string.IsNullOrWhiteSpace(p.Description) ? "" : $" | {p.Description}";
                sb.AppendLine($"- ID:{p.ProductId} | {p.Name} | {p.Price:N0} VND | Loại: {p.Category}{desc}");
            }
            return sb.ToString();
        }

        public async Task<string> BuildRelevantCatalogTextAsync(string userQuery, int take = 10, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userQuery))
                return await BuildCatalogTextAsync(ct);

            bool hasBakeryKeyword = Regex.IsMatch(userQuery, @"bánh|banh|menu|danh mục|danh muc|giá|gia|kem|socola|matcha|dâu|dau|mousse|tiramisu|bông lan|bong lan|phô mai|pho mai|quà|sinh nhật|tiệc", RegexOptions.IgnoreCase);
            var items = await _hybridSearch.SearchProductsAsync(userQuery, take, fallbackToCatalog: hasBakeryKeyword, ct);
            var promos = await _hybridSearch.SearchPromotionsAsync(userQuery, 3, ct);
            var news = await _hybridSearch.SearchNewsAsync(userQuery, 2, ct);

            var sb = new StringBuilder();
            sb.AppendLine("DANH MỤC SẢN PHẨM PHÙ HỢP (RAG HYBRID SEARCH):");
            if (items.Count > 0)
            {
                foreach (var p in items)
                {
                    var desc = string.IsNullOrWhiteSpace(p.Description) ? "" : $" | {p.Description}";
                    sb.AppendLine($"- ID:{p.ProductId} | {p.Name} | {p.Price:N0} VND | Loại: {p.Category}{desc}");
                }
            }
            else
            {
                sb.AppendLine("- (Không tìm thấy bánh trùng khớp trực tiếp từ khóa, chỉ tư vấn trong phạm vi tiệm)");
            }

            if (promos.Count > 0)
            {
                sb.AppendLine("\nKHUYẾN MÃI / ƯU ĐÃI ĐANG DIỄN RA LIÊN QUAN:");
                foreach (dynamic promo in promos)
                {
                    sb.AppendLine($"- {promo.title}: {promo.description} (Giảm {promo.discountPercent}%, đến ngày {promo.endDate})");
                }
            }

            if (news.Count > 0)
            {
                sb.AppendLine("\nTIN TỨC / BÍ QUYẾT BẢO QUẢN LIÊN QUAN:");
                foreach (dynamic n in news)
                {
                    sb.AppendLine($"- {n.title}: {n.summary}");
                }
            }

            return sb.ToString();
        }
    }
}
