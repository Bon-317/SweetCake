using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Tìm kiếm thông minh với khả năng chuẩn hóa tiếng Việt, tìm kiếm mờ (fuzzy matching),
    /// và xếp hạng dựa trên độ liên quan.
    /// </summary>
    public interface ISmartSearchService
    {
        /// <summary>
        /// Tìm kiếm sản phẩm bằng thuật toán nhận diện tiếng Việt thông minh.
        /// Trả về danh sách kết quả được sắp xếp giảm dần theo điểm liên quan.
        /// </summary>
        Task<List<ProductSearchResult>> SearchAsync(string? query, int maxResults = 50);

        /// <summary>
        /// Gợi ý tự động điền (Autocomplete) cho thanh tìm kiếm.
        /// </summary>
        Task<List<string>> AutocompleteAsync(string? query, int maxResults = 8);
    }

    public class ProductSearchResult
    {
        public Product Product { get; set; } = null!;
        public double RelevanceScore { get; set; }
    }

    public class SmartSearchService : ISmartSearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVietnameseNormalizerService _normalizer;

        public SmartSearchService(ApplicationDbContext context, IVietnameseNormalizerService normalizer)
        {
            _context = context;
            _normalizer = normalizer;
        }

        public async Task<List<ProductSearchResult>> SearchAsync(string? query, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                var allProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .OrderBy(p => p.ProductId)
                    .Take(maxResults)
                    .ToListAsync();

                return allProducts.Select(p => new ProductSearchResult
                {
                    Product = p,
                    RelevanceScore = 0.0
                }).ToList();
            }

            // Tải toàn bộ sản phẩm và chấm điểm phía server để thực hiện tìm kiếm mờ
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .ToListAsync();

            var results = new List<ProductSearchResult>();

            foreach (var product in products)
            {
                // Chấm điểm dựa trên Tên sản phẩm (trọng số: 1.0)
                var nameScore = _normalizer.Score(product.ProductName, query);

                // Chấm điểm dựa trên Mô tả sản phẩm (trọng số: 0.5)
                var descScore = _normalizer.Score(product.Description, query) * 0.5;

                // Chấm điểm dựa trên Tên danh mục (trọng số: 0.3)
                var catScore = _normalizer.Score(product.Category?.CategoryName, query) * 0.3;

                var totalScore = Math.Max(nameScore, Math.Max(descScore, catScore));

                if (totalScore >= 0.15)
                {
                    results.Add(new ProductSearchResult
                    {
                        Product = product,
                        RelevanceScore = totalScore
                    });
                }
            }

            if (results.Any())
            {
                var maxScore = results.Max(r => r.RelevanceScore);
                // Nếu người dùng gõ đúng tên 1 loại bánh cụ thể (điểm cao >= 0.85), chỉ hiển thị món đó (lọc bỏ các món liên quan yếu)
                // Còn nếu gõ từ khóa chung (bánh kem, bánh bông lan...), điểm cao nhất < 0.85 hoặc các món đều đạt >= 0.75 thì hiện tất cả
                if (maxScore >= 0.85)
                {
                    results = results.Where(r => r.RelevanceScore >= 0.75).ToList();
                }
            }

            return results
                .OrderByDescending(r => r.RelevanceScore)
                .Take(maxResults)
                .ToList();
        }

        public async Task<List<string>> AutocompleteAsync(string? query, int maxResults = 8)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<string>();

            var products = await _context.Products
                .AsNoTracking()
                .Select(p => p.ProductName)
                .ToListAsync();

            return products
                .Where(name => _normalizer.FuzzyContains(name, query) || _normalizer.Score(name, query) >= 0.2)
                .OrderByDescending(name => _normalizer.Score(name, query))
                .ThenBy(name => name)
                .Take(maxResults)
                .ToList();
        }
    }
}
