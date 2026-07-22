using System.Text;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;

namespace SweetCakeShop.Services.AI.Rag
{
    public interface IHybridRagSearchService
    {
        Task<IReadOnlyList<ProductFactDto>> SearchProductsAsync(string query, int limit = 8, bool fallbackToCatalog = true, CancellationToken ct = default);
        Task<IReadOnlyList<ProductFactDto>> RecommendProductsAsync(string? occasion, string? flavor, decimal? maxPrice, int limit = 8, CancellationToken ct = default);
        Task<IReadOnlyList<object>> SearchNewsAsync(string? keyword, int limit = 3, CancellationToken ct = default);
        Task<IReadOnlyList<object>> SearchPromotionsAsync(string? keyword, int limit = 3, CancellationToken ct = default);
    }

    public class HybridRagSearchService : IHybridRagSearchService
    {
        private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "bánh", "banh", "cho", "em", "anh", "chị", "chi", "tôi", "toi", "cần", "can", "tìm", "tim",
            "muốn", "muon", "giúp", "giup", "có", "co", "bán", "ban", "không", "khong", "ạ", "a",
            "nhé", "nhe", "với", "voi", "loại", "loai", "món", "mon", "giá", "gia", "bao", "nhiêu",
            "nhieu", "hỏi", "hoi", "xem", "được", "duoc", "hay", "và", "va", "của", "cua", "là", "la"
        };

        private readonly ApplicationDbContext _context;

        public HybridRagSearchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<ProductFactDto>> SearchProductsAsync(string query, int limit = 8, bool fallbackToCatalog = true, CancellationToken ct = default)
        {
            var term = (query ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(term))
            {
                return fallbackToCatalog ? await GetCatalogFallbackAsync(limit, ct) : new List<ProductFactDto>();
            }

            // Fetch active catalog items joined with categories and sold counts
            var rawProducts = await (
                from p in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                join od in _context.OrderDetails.AsNoTracking() on p.ProductId equals od.ProductId into ods
                select new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Price,
                    p.Description,
                    p.Image,
                    c.CategoryName,
                    SoldQuantity = ods.Sum(x => (int?)x.Quantity) ?? 0
                }).ToListAsync(ct);

            var tokens = Tokenize(term);
            var scored = new List<(double Score, ProductFactDto Dto)>();

            foreach (var p in rawProducts)
            {
                double score = 0;
                var nameLower = p.ProductName.ToLowerInvariant();
                var catLower = (p.CategoryName ?? string.Empty).ToLowerInvariant();
                var descLower = (p.Description ?? string.Empty).ToLowerInvariant();
                var termLower = term.ToLowerInvariant();

                // Exact phrase matching bonus
                if (nameLower.Contains(termLower)) score += 100;
                if (catLower.Contains(termLower)) score += 60;
                if (descLower.Contains(termLower)) score += 30;

                // Token scoring (TF-like)
                int matchedTokens = 0;
                foreach (var token in tokens)
                {
                    bool tokenMatched = false;
                    if (nameLower.Contains(token))
                    {
                        score += 25;
                        tokenMatched = true;
                    }
                    if (catLower.Contains(token))
                    {
                        score += 20;
                        tokenMatched = true;
                    }
                    if (descLower.Contains(token))
                    {
                        score += 10;
                        tokenMatched = true;
                    }

                    if (tokenMatched) matchedTokens++;
                }

                // Multi-token completeness bonus
                if (tokens.Count > 1 && matchedTokens == tokens.Count)
                {
                    score += 40;
                }

                if (score > 0)
                {
                    // Popularity tie-breaker only when query actually matched
                    score += Math.Min(p.SoldQuantity * 0.2, 15.0);
                    scored.Add((score, new ProductFactDto
                    {
                        ProductId = p.ProductId,
                        Name = p.ProductName,
                        Price = p.Price,
                        Category = p.CategoryName,
                        Description = p.Description,
                        ImageUrl = p.Image,
                        SoldQuantity = p.SoldQuantity
                    }));
                }
            }

            if (scored.Count == 0)
            {
                // If token scoring found nothing, check fallback flag before defaulting to top selling
                return fallbackToCatalog ? await GetCatalogFallbackAsync(limit, ct) : new List<ProductFactDto>();
            }

            return scored
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Dto.SoldQuantity)
                .Take(limit)
                .Select(x => x.Dto)
                .ToList();
        }

        public async Task<IReadOnlyList<ProductFactDto>> RecommendProductsAsync(string? occasion, string? flavor, decimal? maxPrice, int limit = 8, CancellationToken ct = default)
        {
            var rawProducts = await (
                from p in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                join od in _context.OrderDetails.AsNoTracking() on p.ProductId equals od.ProductId into ods
                select new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Price,
                    p.Description,
                    p.Image,
                    c.CategoryName,
                    SoldQuantity = ods.Sum(x => (int?)x.Quantity) ?? 0
                }).ToListAsync(ct);

            var flavorTokens = Tokenize(flavor ?? string.Empty);
            var occasionLower = (occasion ?? string.Empty).ToLowerInvariant();

            var scored = new List<(double Score, ProductFactDto Dto)>();

            foreach (var p in rawProducts)
            {
                if (maxPrice.HasValue && p.Price > maxPrice.Value)
                    continue;

                double score = 10; // Base score for being within budget
                var nameLower = p.ProductName.ToLowerInvariant();
                var catLower = (p.CategoryName ?? string.Empty).ToLowerInvariant();
                var descLower = (p.Description ?? string.Empty).ToLowerInvariant();

                // Flavor token matching
                foreach (var token in flavorTokens)
                {
                    if (nameLower.Contains(token)) score += 30;
                    if (catLower.Contains(token)) score += 25;
                    if (descLower.Contains(token)) score += 15;
                }

                // Occasion matching
                if (!string.IsNullOrWhiteSpace(occasionLower))
                {
                    if ((occasionLower.Contains("sinh nhật") || occasionLower.Contains("birthday")) &&
                        (catLower.Contains("sinh nhật") || nameLower.Contains("sinh nhật") || descLower.Contains("sinh nhật")))
                    {
                        score += 50;
                    }
                    else if ((occasionLower.Contains("cưới") || occasionLower.Contains("wedding")) &&
                             (catLower.Contains("cưới") || nameLower.Contains("cưới") || descLower.Contains("cưới")))
                    {
                        score += 50;
                    }
                    else if ((occasionLower.Contains("quà") || occasionLower.Contains("gift") || occasionLower.Contains("người yêu")) &&
                             (descLower.Contains("quà") || descLower.Contains("tặng") || catLower.Contains("cao cấp")))
                    {
                        score += 35;
                    }
                }

                // Popularity bonus
                score += Math.Min(p.SoldQuantity * 0.3, 20.0);

                scored.Add((score, new ProductFactDto
                {
                    ProductId = p.ProductId,
                    Name = p.ProductName,
                    Price = p.Price,
                    Category = p.CategoryName,
                    Description = p.Description,
                    ImageUrl = p.Image,
                    SoldQuantity = p.SoldQuantity
                }));
            }

            if (scored.Count == 0)
            {
                return await GetCatalogFallbackAsync(limit, ct);
            }

            return scored
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Dto.SoldQuantity)
                .Take(limit)
                .Select(x => x.Dto)
                .ToList();
        }

        public async Task<IReadOnlyList<object>> SearchNewsAsync(string? keyword, int limit = 3, CancellationToken ct = default)
        {
            var newsQuery = _context.News.AsNoTracking().Where(n => n.IsPublished);
            var rawNews = await newsQuery.ToListAsync(ct);

            var term = (keyword ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(term))
            {
                return rawNews
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(Math.Clamp(limit, 1, 5))
                    .Select(n => new
                    {
                        title = n.Title,
                        summary = n.Summary,
                        author = n.Author,
                        publishedDate = n.PublishedAt.ToString("dd/MM/yyyy")
                    })
                    .Cast<object>()
                    .ToList();
            }

            var tokens = Tokenize(term);
            var scored = new List<(double Score, object Item)>();

            foreach (var n in rawNews)
            {
                double score = 0;
                var titleLower = (n.Title ?? string.Empty).ToLowerInvariant();
                var sumLower = (n.Summary ?? string.Empty).ToLowerInvariant();
                var contentLower = (n.Content ?? string.Empty).ToLowerInvariant();
                var termLower = term.ToLowerInvariant();

                if (titleLower.Contains(termLower)) score += 100;
                if (sumLower.Contains(termLower)) score += 60;

                foreach (var token in tokens)
                {
                    if (titleLower.Contains(token)) score += 30;
                    if (sumLower.Contains(token)) score += 20;
                    if (contentLower.Contains(token)) score += 10;
                }

                if (score > 0)
                {
                    scored.Add((score, new
                    {
                        title = n.Title,
                        summary = n.Summary,
                        author = n.Author,
                        publishedDate = n.PublishedAt.ToString("dd/MM/yyyy")
                    }));
                }
            }

            if (scored.Count == 0)
            {
                return rawNews
                    .OrderByDescending(n => n.PublishedAt)
                    .Take(Math.Clamp(limit, 1, 5))
                    .Select(n => new
                    {
                        title = n.Title,
                        summary = n.Summary,
                        author = n.Author,
                        publishedDate = n.PublishedAt.ToString("dd/MM/yyyy")
                    })
                    .Cast<object>()
                    .ToList();
            }

            return scored
                .OrderByDescending(x => x.Score)
                .Take(Math.Clamp(limit, 1, 5))
                .Select(x => x.Item)
                .ToList();
        }

        public async Task<IReadOnlyList<object>> SearchPromotionsAsync(string? keyword, int limit = 3, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var rawPromos = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync(ct);

            var term = (keyword ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(term))
            {
                return rawPromos
                    .OrderByDescending(p => p.DiscountPercent)
                    .Take(Math.Clamp(limit, 1, 5))
                    .Select(p => new
                    {
                        title = p.Title,
                        description = p.Description,
                        discountPercent = p.DiscountPercent,
                        badge = p.BadgeText,
                        endDate = p.EndDate.ToString("dd/MM/yyyy")
                    })
                    .Cast<object>()
                    .ToList();
            }

            var tokens = Tokenize(term);
            var scored = new List<(double Score, object Item)>();

            foreach (var p in rawPromos)
            {
                double score = 0;
                var titleLower = (p.Title ?? string.Empty).ToLowerInvariant();
                var descLower = (p.Description ?? string.Empty).ToLowerInvariant();
                var badgeLower = (p.BadgeText ?? string.Empty).ToLowerInvariant();
                var termLower = term.ToLowerInvariant();

                if (titleLower.Contains(termLower)) score += 100;
                if (descLower.Contains(termLower)) score += 50;

                foreach (var token in tokens)
                {
                    if (titleLower.Contains(token)) score += 30;
                    if (descLower.Contains(token)) score += 20;
                    if (badgeLower.Contains(token)) score += 15;
                }

                if (score > 0)
                {
                    score += (double)p.DiscountPercent * 0.5; // Higher discounts get priority
                    scored.Add((score, new
                    {
                        title = p.Title,
                        description = p.Description,
                        discountPercent = p.DiscountPercent,
                        badge = p.BadgeText,
                        endDate = p.EndDate.ToString("dd/MM/yyyy")
                    }));
                }
            }

            if (scored.Count == 0)
            {
                return rawPromos
                    .OrderByDescending(p => p.DiscountPercent)
                    .Take(Math.Clamp(limit, 1, 5))
                    .Select(p => new
                    {
                        title = p.Title,
                        description = p.Description,
                        discountPercent = p.DiscountPercent,
                        badge = p.BadgeText,
                        endDate = p.EndDate.ToString("dd/MM/yyyy")
                    })
                    .Cast<object>()
                    .ToList();
            }

            return scored
                .OrderByDescending(x => x.Score)
                .Take(Math.Clamp(limit, 1, 5))
                .Select(x => x.Item)
                .ToList();
        }

        private async Task<IReadOnlyList<ProductFactDto>> GetCatalogFallbackAsync(int take, CancellationToken ct)
        {
            return await (
                from p in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                orderby p.ProductName
                select new ProductFactDto
                {
                    ProductId = p.ProductId,
                    Name = p.ProductName,
                    Price = p.Price,
                    Category = c.CategoryName,
                    ImageUrl = p.Image
                }).Take(take).ToListAsync(ct);
        }

        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();

            var normalized = text.ToLowerInvariant();
            var words = normalized.Split(new[] { ' ', '\t', '\r', '\n', ',', '.', '?', '!', ';', ':', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            var tokens = new List<string>();
            foreach (var w in words)
            {
                var clean = w.Trim();
                if (clean.Length > 1 && !Stopwords.Contains(clean))
                {
                    tokens.Add(clean);
                }
            }
            return tokens;
        }
    }
}
