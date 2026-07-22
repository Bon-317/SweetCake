using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Models.Api;
using SweetCakeShop.Services.AI;
using SweetCakeShop.Services.AI.Rag;

namespace SweetCakeShop.Services.Chat
{
    public class ProductQueryResult
    {
        public bool UseDirectReply { get; set; }
        public string DirectReply { get; set; } = string.Empty;
        public List<ProductFactDto> Products { get; set; } = [];
        public string FactsBlock { get; set; } = string.Empty;
    }

    public interface IProductIntentResolver
    {
        Task<ProductQueryResult> ResolveAsync(
            string userMessage,
            int? pageProductId,
            IReadOnlyList<CustomerChatMessage> recentHistory,
            CancellationToken ct = default);
    }

    public class ProductIntentResolver : IProductIntentResolver
    {
        private static readonly string[] StopWords =
        [
            "bánh", "banh", "bạn", "ban", "có", "co", "không", "khong", "cho", "tôi", "toi",
            "mình", "minh", "em", "ạ", "a", "nhé", "nhe", "ơi", "oi", "hay", "loại", "loai",
            "món", "mon", "nào", "nao", "gì", "gi", "muốn", "muon", "hỏi", "hoi", "xin",
            "vui", "lòng", "long", "shop", "tiệm", "tiem", "cửa", "cua", "hàng", "hang",
            "sweetcakeshop", "dạ", "da", "the", "an", "is", "are", "do", "you", "have",
            "tối", "nay", "anh", "chị", "làm", "tiệc", "sinh", "nhật", "bạn", "gái", "mua",
            "chiếc", "giá", "tầm", "dưới", "trên", "tư", "vấn", "mẫu", "sang", "đẹp", "với",
            "khuyến", "mãi", "giảm", "chương", "trình", "hấp", "dẫn", "hiện", "tại", "đang"
        ];

        private readonly ApplicationDbContext _db;
        private readonly IProductAnalyticsService _analytics;
        private readonly IHybridRagSearchService _hybridSearch;
        private readonly IProductCatalogForAiService _catalog;
        private readonly IRecommendationService _recommendations;

        public ProductIntentResolver(
            ApplicationDbContext db,
            IProductAnalyticsService analytics,
            IHybridRagSearchService hybridSearch,
            IProductCatalogForAiService catalog,
            IRecommendationService recommendations)
        {
            _db = db;
            _analytics = analytics;
            _hybridSearch = hybridSearch;
            _catalog = catalog;
            _recommendations = recommendations;
        }

        public async Task<ProductQueryResult> ResolveAsync(
            string userMessage,
            int? pageProductId,
            IReadOnlyList<CustomerChatMessage> recentHistory,
            CancellationToken ct = default)
        {
            var t = userMessage.Trim().ToLowerInvariant();

            // 1. Nếu hỏi về khuyến mãi, giảm giá, voucher, ưu đãi, chương trình, sale
            if (Regex.IsMatch(t, @"khuyến mãi|khuyen mai|giảm giá|giam gia|flash sale|voucher|ưu đãi|uu dai|chương trình|chuong trinh|sale"))
            {
                // Kiểm tra xem khách có hỏi giảm giá cho một món bánh cụ thể hay không (VD: bánh tiramisu có giảm giá không / món này có giảm không)
                ProductFactDto? targetDiscountProduct = null;
                if (pageProductId.HasValue && Regex.IsMatch(t, @"món này|bánh này|sản phẩm này|ở trên"))
                {
                    targetDiscountProduct = await GetByIdAsync(pageProductId.Value, ct);
                }
                else
                {
                    var cleanPhrase = Regex.Replace(t, @"khuyến mãi|khuyen mai|giảm giá|giam gia|flash sale|voucher|ưu đãi|uu dai|chương trình|chuong trinh|sale|hiện|có|đang|không|hay|bánh|banh|món|mon|nào|nao|tiệm|tiem|shop|cho|hỏi|với", "", RegexOptions.IgnoreCase).Trim();
                    var cakePhrase = ExtractSearchPhrase(cleanPhrase);
                    if (!string.IsNullOrWhiteSpace(cakePhrase) && cakePhrase.Length >= 2)
                    {
                        var hits = await SearchByPhraseAsync(cakePhrase, 2, ct);
                        if (hits.Count > 0)
                            targetDiscountProduct = hits[0];
                    }
                }

                if (targetDiscountProduct != null)
                {
                    var allPromos = await _hybridSearch.SearchPromotionsAsync(null, 10, ct);
                    var matchedPromos = new List<dynamic>();
                    var generalPromos = new List<dynamic>();

                    foreach (dynamic p in allPromos)
                    {
                        string title = (p.title ?? "").ToString();
                        string desc = (p.description ?? "").ToString();
                        string badge = (p.badge ?? "").ToString();
                        string comb = $"{title} {desc} {badge}".ToLowerInvariant();

                        if (comb.Contains(targetDiscountProduct.Name.ToLowerInvariant()) ||
                            (!string.IsNullOrWhiteSpace(targetDiscountProduct.Category) && comb.Contains(targetDiscountProduct.Category.ToLowerInvariant())))
                        {
                            matchedPromos.Add(p);
                        }
                        else if (comb.Contains("tất cả") || comb.Contains("toàn bộ") || comb.Contains("đơn từ") || comb.Contains("đơn tối thiểu") || comb.Contains("flash sale"))
                        {
                            generalPromos.Add(p);
                        }
                    }

                    if (matchedPromos.Count > 0)
                    {
                        var promoLines = matchedPromos.Select(p => $"🎁 **{p.title}**: {p.description} *(Giảm {p.discountPercent}% - Đến ngày {p.endDate})*");
                        return new ProductQueryResult
                        {
                            UseDirectReply = true,
                            DirectReply = $"Dạ, với dòng **{targetDiscountProduct.Name}** ({targetDiscountProduct.Price:N0} VND), hiện đang có chương trình ưu đãi áp dụng ngay ạ:\n\n{string.Join("\n\n", promoLines)}\n\nAnh/chị nhấn vào mẫu bánh bên dưới để em hỗ trợ đặt hàng và áp dụng ưu đãi tốt nhất cho mình nhé!",
                            Products = [targetDiscountProduct],
                            FactsBlock = $"ANSWER_PRODUCT_PROMO: {targetDiscountProduct.Name} có khuyến mãi riêng."
                        };
                    }
                    else if (generalPromos.Count > 0)
                    {
                        var promoLines = generalPromos.Select(p => $"🎁 **{p.title}**: {p.description} *(Giảm {p.discountPercent}% - Đến ngày {p.endDate})*");
                        return new ProductQueryResult
                        {
                            UseDirectReply = true,
                            DirectReply = $"Dạ, hiện mẫu **{targetDiscountProduct.Name}** ({targetDiscountProduct.Price:N0} VND) không có ưu đãi giảm giá riêng, nhưng anh/chị có thể áp dụng các chương trình ưu đãi chung cực kỳ hấp dẫn của SweetCakeShop khi đặt món này ạ:\n\n{string.Join("\n\n", promoLines)}\n\nAnh/chị muốn đặt **{targetDiscountProduct.Name}** với ưu đãi trên thì nhấn vào bánh bên dưới hoặc báo em ngay nhé!",
                            Products = [targetDiscountProduct],
                            FactsBlock = $"ANSWER_PRODUCT_PROMO: {targetDiscountProduct.Name} áp dụng khuyến mãi chung."
                        };
                    }
                    else
                    {
                        return new ProductQueryResult
                        {
                            UseDirectReply = true,
                            DirectReply = $"Dạ, hiện tại dòng **{targetDiscountProduct.Name}** ({targetDiscountProduct.Price:N0} VND) chưa có chương trình giảm giá riêng nào đang diễn ra ạ. Tuy nhiên mức giá này đã là mức giá ưu đãi nhất với cam kết nguyên liệu hảo hạng làm tươi mỗi ngày tại tiệm! 🍰\n\nAnh/chị có muốn đặt ngay mẫu **{targetDiscountProduct.Name}** này hay tham khảo thêm các dòng bánh khác đang có khuyến mãi không ạ?",
                            Products = [targetDiscountProduct],
                            FactsBlock = $"ANSWER_PRODUCT_PROMO: {targetDiscountProduct.Name} hiện không giảm giá."
                        };
                    }
                }

                var promosGeneral = await _hybridSearch.SearchPromotionsAsync(userMessage, 5, ct);
                if (promosGeneral.Count > 0)
                {
                    var promoLines = new List<string>();
                    foreach (dynamic promo in promosGeneral)
                    {
                        promoLines.Add($"🎁 **{promo.title}**: {promo.description} *(Giảm {promo.discountPercent}% - Đến ngày {promo.endDate})*");
                    }
                    return new ProductQueryResult
                    {
                        UseDirectReply = true,
                        DirectReply = $"Dạ, hiện tại SweetCakeShop đang có các chương trình khuyến mãi & ưu đãi hấp dẫn sau ạ:\n\n{string.Join("\n\n", promoLines)}\n\nAnh/chị chọn bánh cho dịp gì hay tầm giá bao nhiêu để em áp dụng ưu đãi tốt nhất cho mình nhé!",
                        FactsBlock = $"ANSWER_PROMOTIONS: Có {promosGeneral.Count} chương trình khuyến mãi đang diễn ra."
                    };
                }
                return new ProductQueryResult
                {
                    UseDirectReply = true,
                    DirectReply = "Dạ, hiện tại tiệm em tạm thời chưa có chương trình khuyến mãi hay Flash Sale riêng nào đang diễn ra ạ. Tuy nhiên các mẫu bánh tại SweetCakeShop luôn cam kết mức giá cực kỳ hợp lý cùng chất lượng nguyên liệu tươi ngon nhất mỗi ngày! 🍰\n\nAnh/chị đang muốn chọn bánh cho dịp gì hay tầm ngân sách bao nhiêu để em gợi ý cho mình nhé ạ!",
                    FactsBlock = "ANSWER_PROMOTIONS: Hiện không có chương trình khuyến mãi nào."
                };
            }

            // 2. Nếu hỏi tổng số loại bánh, có bao nhiêu loại, menu có gì
            if (Regex.IsMatch(t, @"bao nhiêu loại|bao nhieu loai|tổng số bánh|tong so banh|bao nhiêu món|bao nhieu mon|có những loại|co nhung loai|menu có gì|danh mục bánh|danh muc banh|tất cả bánh|tat ca banh"))
            {
                var allProducts = await _catalog.GetAllProductsAsync(ct);
                var totalProducts = allProducts.Count;
                var categories = allProducts.Select(p => p.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
                var totalCategories = categories.Count;
                var catListStr = string.Join(", ", categories);
                var sampleProducts = allProducts.Take(4).ToList();

                return new ProductQueryResult
                {
                    UseDirectReply = true,
                    DirectReply = $"Dạ, menu SweetCakeShop hiện có tất cả **{totalProducts} loại bánh** phong phú trải dài ở **{totalCategories} danh mục** chính: **{catListStr}** 🍰🎂\n\nTừ bánh kem sinh nhật, Tiramisu, bánh phô mai đến mousse trái cây đều được tiệm chuẩn bị tươi ngon mỗi ngày. Anh/chị muốn tìm bánh cho dịp gì hay tầm ngân sách bao nhiêu để em đề xuất mẫu phù hợp nhất nhé!",
                    Products = sampleProducts,
                    FactsBlock = $"ANSWER_TOTAL_PRODUCTS: Tiệm có {totalProducts} loại bánh trong {totalCategories} danh mục ({catListStr})."
                };
            }

            // 3. Nếu hỏi về ngân sách / đề xuất bánh theo ngân sách / tiền
            if (Regex.IsMatch(t, @"ngân sách|ngan sach|tầm tiền|tam tien|khoảng tiền|khoang tien|đề xuất.*tiền|đề xuất.*ngân|bao nhiêu tiền thì|mua được bánh gì|tư vấn.*tiền|dưới \d+|tầm \d+|khoảng \d+|max \d+|\d+\s*(?:k|nghìn|ngan|trăm|tram|đ|dong|đồng|vnd)"))
            {
                decimal? maxPrice = null;
                var numMatch = Regex.Match(t, @"(\d+)(?:\.\d+)*(?:\s*(?:k|nghìn|ngan|trăm|tram|vnd|đ|dong|đồng))?");
                if (numMatch.Success && decimal.TryParse(numMatch.Groups[1].Value, out var val))
                {
                    if (t.Contains("k") || t.Contains("nghìn") || t.Contains("ngan") || val < 1000)
                        maxPrice = val * 1000;
                    else
                        maxPrice = val;
                }

                if (maxPrice.HasValue && maxPrice.Value >= 10000)
                {
                    string? occ = Regex.IsMatch(t, @"sinh nhật|sinh nhat") ? "sinh nhật" : (Regex.IsMatch(t, @"tiệc|tiec|cưới|cuoi") ? "tiệc" : null);
                    string? flav = Regex.IsMatch(t, @"socola|choco") ? "socola" : (Regex.IsMatch(t, @"matcha|trà") ? "matcha" : (Regex.IsMatch(t, @"dâu|dau|trái cây|trai cay") ? "trái cây" : null));
                    var recommended = await _recommendations.RecommendWithPreferencesAsync(occ, flav, maxPrice.Value, 4, ct);
                    if (recommended.Count > 0)
                    {
                        return new ProductQueryResult
                        {
                            UseDirectReply = true,
                            DirectReply = $"Dạ, với ngân sách khoảng **{maxPrice.Value:N0} VND**, em xin đề xuất các mẫu bánh cực kỳ thơm ngon, vừa tầm tiền và phù hợp nhất bên dưới ạ 🍰🎂\n\nAnh/chị ưng ý mẫu nào cứ nhấn vào để xem chi tiết hoặc bảo em hỗ trợ đặt bánh giao tận nơi nhé!",
                            Products = recommended.ToList(),
                            FactsBlock = $"ANSWER_BUDGET: Đề xuất {recommended.Count} bánh dưới {maxPrice.Value:N0} VND."
                        };
                    }
                    return new ProductQueryResult
                    {
                        UseDirectReply = true,
                        DirectReply = $"Dạ, hiện tại với mức giá dưới **{maxPrice.Value:N0} VND** tiệm chưa có mẫu bánh kem lớn, nhưng tiệm có sẵn các dòng bánh ngọt mini và trà chiều cực kỳ hấp dẫn ạ! Anh/chị có muốn tham khảo các mẫu bánh từ 150.000 VND không ạ?",
                        FactsBlock = $"ANSWER_BUDGET: Không có bánh dưới {maxPrice.Value:N0} VND."
                    };
                }

                if (Regex.IsMatch(t, @"ngân sách hiện tại là bao nhiêu|hỏi ngân sách|tư vấn theo ngân sách"))
                {
                    var all = await _catalog.GetAllProductsAsync(ct);
                    var minPrice = all.Min(p => p.Price);
                    var maxPriceCat = all.Max(p => p.Price);
                    return new ProductQueryResult
                    {
                        UseDirectReply = true,
                        DirectReply = $"Dạ, các mẫu bánh tại SweetCakeShop có mức giá rất đa dạng, dao động từ **{minPrice:N0} VND** (bánh ngọt mini/trà chiều) đến **{maxPriceCat:N0} VND** (bánh kem sinh nhật cao cấp/tiệc lớn) ạ! 🍰\n\nĐể em đề xuất chính xác nhất, anh/chị cho em biết **ngân sách dự kiến của mình** (VD: dưới 300k, hay khoảng 350k - 450k) cùng **dịp sử dụng** (sinh nhật, kỷ niệm, quà tặng) nhé ạ!",
                        Products = all.Take(4).ToList(),
                        FactsBlock = $"ANSWER_BUDGET_GUIDE: Giá từ {minPrice:N0} VND đến {maxPriceCat:N0} VND."
                    };
                }
            }

            // 4. Nếu hỏi gợi ý / đề xuất / tư vấn chọn bánh chung chung
            if (Regex.IsMatch(t, @"gợi ý|goi y|đề xuất|de xuat|tư vấn|tu van|nên mua|nen mua|nên chọn|nen chon|quà tặng|qua tang|sinh nhật|sinh nhat|tiệc|tiec|bạn gái|ban gai|người yêu|nguoi yeu"))
            {
                string? occ = Regex.IsMatch(t, @"sinh nhật|sinh nhat") ? "sinh nhật" : (Regex.IsMatch(t, @"tiệc|tiec|cưới|cuoi") ? "tiệc" : (Regex.IsMatch(t, @"quà|qua|tặng|tang|bạn gái|ban gai|người yêu|nguoi yeu") ? "quà tặng" : null));
                string? flav = Regex.IsMatch(t, @"socola|choco") ? "socola" : (Regex.IsMatch(t, @"matcha|trà") ? "matcha" : (Regex.IsMatch(t, @"dâu|dau|trái cây|trai cay") ? "trái cây" : (Regex.IsMatch(t, @"phô mai|pho mai|cheese") ? "phô mai" : null)));
                var recs = await _recommendations.RecommendWithPreferencesAsync(occ, flav, null, 4, ct);
                if (recs.Count > 0)
                {
                    var reason = occ != null ? $"cho dịp **{occ}**" : (flav != null ? $"với hương vị **{flav}**" : "được yêu thích nhất");
                    return new ProductQueryResult
                    {
                        UseDirectReply = true,
                        DirectReply = $"Dạ, em xin gợi ý/đề xuất những dòng bánh thơm ngon và nổi bật nhất {reason} tại SweetCakeShop ngay bên dưới ạ 🎂🍰:\n\nAnh/chị thích mẫu bánh nào để em giới thiệu chi tiết thêm cho mình nhé!",
                        Products = recs.ToList(),
                        FactsBlock = $"ANSWER_RECOMMEND: Đề xuất {recs.Count} bánh phù hợp."
                    };
                }
            }

            // 5. Kiểm tra giá cao nhất / thấp nhất / bán chạy / tương tự như cũ
            if (Regex.IsMatch(t, @"đắt nhất|dat nhat|expensive|giá cao nhất|gia cao nhat|cao nhất|cao nhat"))
                return await SingleProductReply(await _analytics.GetHighestPriceAsync(ct), "đắt nhất");

            if (Regex.IsMatch(t, @"rẻ nhất|re nhat|cheapest|giá thấp|gia thap|ít tiền|it tien"))
                return await SingleProductReply(await _analytics.GetLowestPriceAsync(ct), "rẻ nhất");

            if (Regex.IsMatch(t, @"bán chạy|ban chay|best sell|phổ biến|pho bien"))
            {
                var top = (await _analytics.GetTopSellingAsync(1, ct)).FirstOrDefault();
                return await SingleProductReply(top, "bán chạy nhất");
            }

            if (Regex.IsMatch(t, @"tương tự|tuong tu|giống|giong|same|like above|như trên|nhu tren"))
            {
                var anchorId = pageProductId ?? await ResolveAnchorProductIdAsync(recentHistory, ct);
                if (anchorId.HasValue)
                {
                    var anchor = await GetByIdAsync(anchorId.Value, ct);
                    if (anchor != null)
                    {
                        var related = await _analytics.GetRelatedProductsAsync(anchor.Name, 3, ct);
                        if (related.Count == 0)
                            return new ProductQueryResult { UseDirectReply = false, FactsBlock = $"Sản phẩm anchor: {anchor.Name} nhưng chưa có món tương tự." };
                        return MultiProductReply(related.Take(3).ToList(),
                            $"Dạ, các món tương tự **{anchor.Name}** 🎂:");
                    }
                }
                return new ProductQueryResult { UseDirectReply = false, FactsBlock = "Khách hỏi bánh tương tự nhưng chưa rõ món anchor." };
            }

            // 6. Nếu câu hỏi có từ khóa hoặc cụm từ cụ thể, thử tra cứu bằng Hybrid RAG (với fallbackToCatalog = false)
            var searchPhrase = ExtractSearchPhrase(userMessage);
            if (!string.IsNullOrWhiteSpace(searchPhrase) && searchPhrase.Length >= 2)
            {
                var hits = await SearchByPhraseAsync(searchPhrase, 4, ct);
                if (hits.Count == 1)
                    return await SingleProductReply(hits[0], null);
                if (hits.Count > 1)
                    return MultiProductReply(hits.Take(3).ToList(), "Dạ, em tìm thấy các bánh phù hợp bên dưới ạ:");
            }

            // 7. KIỂM TRA CHẶT CHẼ CÂU HỎI KHÔNG LIÊN QUAN (Tuyệt đối KHÔNG fallback về top 3 bánh bán chạy nhất)
            bool hasBakeryContext = Regex.IsMatch(t, @"bánh|banh|cake|kem|socola|matcha|dâu|dau|mousse|tiramisu|bông lan|bong lan|phô mai|pho mai|hương vị|huong vi|ngọt|ngot|đặt hàng|dat hang|giao hàng|giao hang|tiệm|tiem|cửa hàng|cua hang|menu|thực đơn|thuc don|giá|gia|bán|ban", RegexOptions.IgnoreCase);

            if (!hasBakeryContext)
            {
                // Nếu khách hỏi câu hoàn toàn không liên quan đến tiệm bánh (VD: "mày ngu", "thời tiết thế nào", "1+1 bằng mấy")
                return new ProductQueryResult
                {
                    UseDirectReply = true,
                    DirectReply = "Dạ, em là trợ lý AI chuyên tư vấn bánh của SweetCakeShop nên em chỉ xin phép trò chuyện và hỗ trợ anh/chị các thông tin về menu bánh, giá cả, khuyến mãi hoặc đặt bánh tại tiệm thôi ạ! 🍰\n\nAnh/chị đang muốn tham khảo bánh kem sinh nhật, trà chiều hay cần em hỗ trợ gì về tiệm thì bảo em ngay nhé!",
                    Products = [] // Trả về danh sách rỗng 0 sản phẩm, KHÔNG FALLBACK TOP 3 BÁNH BÁN CHẠY!
                };
            }

            // Mặc định cho các câu hỏi ngữ cảnh khác thuộc tiệm bánh: chuyển AI xử lý thông minh
            return new ProductQueryResult
            {
                UseDirectReply = false,
                FactsBlock = "Chuyển xử lý cho AI + Hybrid RAG."
            };
        }

        private async Task<ProductQueryResult> SingleProductReply(
            ProductFactDto? p, string? label, string? prefix = null)
        {
            if (p == null)
                return new ProductQueryResult { UseDirectReply = false, FactsBlock = "Không tìm thấy sản phẩm cụ thể." };

            var cat = string.IsNullOrWhiteSpace(p.Category) ? "" : $" ({p.Category})";
            var head = prefix ?? (label != null ? $"Dạ, bánh {label} là" : "Dạ");
            return new ProductQueryResult
            {
                UseDirectReply = true,
                DirectReply = $"{head} **{p.Name}** 🍰 — **{p.Price:N0} VND**{cat} ạ.",
                Products = [p],
                FactsBlock = $"ANSWER_PRODUCT: {p.Name} | {p.Price:N0} VND | {p.Category}"
            };
        }

        private static ProductQueryResult MultiProductReply(List<ProductFactDto> items, string head)
        {
            var lines = items.Select(p =>
            {
                var cat = string.IsNullOrWhiteSpace(p.Category) ? "" : $" ({p.Category})";
                return $"**{p.Name}** 🍰 — {p.Price:N0} VND{cat}";
            });
            return new ProductQueryResult
            {
                UseDirectReply = true,
                DirectReply = $"{head}\n{string.Join("\n", lines)}",
                Products = items,
                FactsBlock = string.Join("\n", items.Select(p => $"PRODUCT: {p.Name} | {p.Price:N0}"))
            };
        }

        private async Task<List<ProductFactDto>> SearchByPhraseAsync(string phrase, int take, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return [];

            var scored = await _hybridSearch.SearchProductsAsync(phrase, take, fallbackToCatalog: false, ct);
            return scored.ToList();
        }

        private static string ExtractSearchPhrase(string message)
        {
            var words = Regex.Split(message.ToLowerInvariant(), @"\s+")
                .Where(w => w.Length > 1 && !StopWords.Contains(w))
                .ToList();
            if (words.Count == 0) return string.Empty;
            return string.Join(" ", words.Take(4));
        }

        private async Task<int?> ResolveAnchorProductIdAsync(
            IReadOnlyList<CustomerChatMessage> history, CancellationToken ct)
        {
            var lastUser = history.LastOrDefault(m => m.Sender == "user" && m.ContextProductId.HasValue);
            if (lastUser?.ContextProductId != null)
                return lastUser.ContextProductId;

            var lastModel = history.LastOrDefault(m => m.Sender == "model");
            if (lastModel == null) return null;

            var names = await _db.Products.AsNoTracking().Select(p => new { p.ProductId, p.ProductName }).ToListAsync(ct);
            foreach (var n in names.OrderByDescending(x => x.ProductName.Length))
            {
                if (lastModel.Content.Contains(n.ProductName, StringComparison.OrdinalIgnoreCase))
                    return n.ProductId;
            }
            return null;
        }

        private async Task<ProductFactDto?> GetByIdAsync(int id, CancellationToken ct) =>
            await (
                from prod in _db.Products.AsNoTracking()
                join c in _db.Categories.AsNoTracking() on prod.CategoryId equals c.CategoryId
                where prod.ProductId == id
                select new ProductFactDto
                {
                    ProductId = prod.ProductId,
                    Name = prod.ProductName,
                    Price = prod.Price,
                    Category = c.CategoryName,
                    Description = prod.Description,
                    ImageUrl = prod.Image
                }).FirstOrDefaultAsync(ct);
    }
}
