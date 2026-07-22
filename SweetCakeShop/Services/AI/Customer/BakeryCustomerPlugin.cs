using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services.AI.Rag;

namespace SweetCakeShop.Services.AI.Customer
{
    /// <summary>
    /// Read-only functions that may be selected by the customer assistant. Business data is
    /// always fetched by application services; the model never receives database access.
    /// </summary>
    public sealed class BakeryCustomerPlugin
    {
        private readonly IProductAnalyticsService _products;
        private readonly IRecommendationService _recommendations;
        private readonly CartService _cart;
        private readonly ICustomerToolCallContext _toolContext;
        private readonly ApplicationDbContext _context;
        private readonly IHybridRagSearchService _hybridSearch;

        public BakeryCustomerPlugin(
            IProductAnalyticsService products,
            IRecommendationService recommendations,
            CartService cart,
            ICustomerToolCallContext toolContext,
            ApplicationDbContext context,
            IHybridRagSearchService hybridSearch)
        {
            _products = products;
            _recommendations = recommendations;
            _cart = cart;
            _toolContext = toolContext;
            _context = context;
            _hybridSearch = hybridSearch;
        }

        [KernelFunction("search_products")]
        [Description("Tìm bánh đang có trong catalog theo tên, hương vị hoặc loại bánh. Luôn dùng hàm này khi khách hỏi có bán bánh nào, hỏi giá hoặc tìm bánh cụ thể.")]
        public async Task<string> SearchProductsAsync(
            [Description("Từ khóa sản phẩm, hương vị hoặc loại bánh do khách nói.")] string query,
            [Description("Số lượng tối đa từ 1 đến 6. Mặc định là 4.")] int limit = 4)
        {
            var products = await _products.SearchProductsAsync(query, Math.Clamp(limit, 1, 6));
            return FormatProducts(products);
        }

        [KernelFunction("get_product_details")]
        [Description("Lấy thông tin và giá hiện tại của một bánh đã biết tên. Dùng khi khách hỏi chi tiết hoặc giá của bánh đó.")]
        public async Task<string> GetProductDetailsAsync(
            [Description("Tên bánh cần xem chi tiết.")] string productName)
        {
            var product = await _products.GetProductDetailsAsync(productName);
            return product == null
                ? JsonSerializer.Serialize(new { found = false, message = "Không tìm thấy bánh trong catalog hiện tại." })
                : FormatProducts([product]);
        }

        [KernelFunction("recommend_products")]
        [Description("Gợi ý bánh theo dịp, khẩu vị và ngân sách. Dùng khi khách muốn tư vấn chọn bánh.")]
        public async Task<string> RecommendProductsAsync(
            [Description("Dịp sử dụng, ví dụ sinh nhật, quà tặng, cưới hoặc tiệc.")] string? occasion = null,
            [Description("Khẩu vị hoặc phong cách, ví dụ socola, trái cây, ít ngọt.")] string? flavor = null,
            [Description("Ngân sách tối đa bằng VND nếu khách có nêu.")] decimal? maxPrice = null,
            [Description("Số lượng tối đa từ 1 đến 6. Mặc định là 4.")] int limit = 4)
        {
            var products = await _recommendations.RecommendWithPreferencesAsync(
                occasion, flavor, maxPrice, Math.Clamp(limit, 1, 6));
            return FormatProducts(products);
        }

        [KernelFunction("get_related_products")]
        [Description("Tìm các bánh cùng loại hoặc tương tự một bánh đang được nhắc tới.")]
        public async Task<string> GetRelatedProductsAsync(
            [Description("Tên bánh gốc để tìm các bánh tương tự.")] string productName,
            [Description("Số lượng tối đa từ 1 đến 6. Mặc định là 4.")] int limit = 4)
        {
            var products = await _products.GetRelatedProductsAsync(productName, Math.Clamp(limit, 1, 6));
            return FormatProducts(products);
        }

        [KernelFunction("get_cart_summary")]
        [Description("Lấy số món và tổng tiền trong giỏ hàng của phiên khách hiện tại.")]
        public string GetCartSummary()
        {
            var cart = _cart.GetCart();
            return JsonSerializer.Serialize(new
            {
                itemCount = cart.Items.Sum(item => item.Quantity),
                total = cart.TotalAmount,
                currency = "VND"
            });
        }

        [KernelFunction("get_delivery_information")]
        [Description("Lấy hướng dẫn giao hàng hiện có của cửa hàng. Không dùng để khẳng định một khung giờ giao còn trống.")]
        public static string GetDeliveryInformation() =>
            "Cửa hàng giao nội thành sau khi xác nhận đơn. Thời gian giao cụ thể cần được nhân viên hoặc hệ thống kiểm tra trước khi xác nhận.";

        [KernelFunction("get_checkout_guide")]
        [Description("Hướng dẫn khách các bước đặt hàng trên website.")]
        public static string GetCheckoutGuide() =>
            "Chọn bánh, thêm vào giỏ, đăng nhập, điền thông tin nhận hàng và chọn thanh toán COD hoặc thanh toán trực tuyến.";

        [KernelFunction("get_active_promotions")]
        [Description("Tìm các chương trình khuyến mãi, ưu đãi hoặc Flash Sale đang diễn ra của tiệm bánh. Luôn gọi hàm này khi khách hỏi có ưu đãi, giảm giá hay khuyến mãi gì không.")]
        public async Task<string> GetActivePromotionsAsync()
        {
            var promos = await _hybridSearch.SearchPromotionsAsync(null, 5);
            if (promos.Count == 0)
                return JsonSerializer.Serialize(new { found = false, message = "Hiện tại tiệm chưa có chương trình khuyến mãi nào đang diễn ra." });

            return JsonSerializer.Serialize(new { found = true, promotions = promos });
        }

        [KernelFunction("search_news")]
        [Description("Tìm kiếm bài viết, tin tức hoặc bí quyết bảo quản bánh của tiệm theo từ khóa.")]
        public async Task<string> SearchNewsAsync(
            [Description("Từ khóa tin tức hoặc chủ đề cần tìm, ví dụ bảo quản, mùa hè, giải thưởng.")] string? keyword = null,
            [Description("Số lượng bài viết tối đa cần lấy từ 1 đến 5. Mặc định là 3.")] int limit = 3)
        {
            var news = await _hybridSearch.SearchNewsAsync(keyword, limit);
            if (news.Count == 0)
                return JsonSerializer.Serialize(new { found = false, message = "Không tìm thấy bài viết hoặc tin tức nào phù hợp." });

            return JsonSerializer.Serialize(new { found = true, news });
        }

        [KernelFunction("check_order_status")]
        [Description("Tra cứu trạng thái và thông tin đơn hàng theo mã đơn hàng hoặc số điện thoại người đặt.")]
        public async Task<string> CheckOrderStatusAsync(
            [Description("Mã đơn hàng do khách cung cấp, ví dụ ORD-15 hoặc số nguyên 15.")] string? orderId = null,
            [Description("Số điện thoại người đặt bánh do khách cung cấp.")] string? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(phoneNumber))
                return JsonSerializer.Serialize(new { found = false, message = "Khách cần cung cấp mã đơn hàng hoặc số điện thoại để kiểm tra trạng thái." });

            var query = _context.Orders.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                var cleanId = orderId.Trim();
                if (cleanId.StartsWith("ORD-", StringComparison.OrdinalIgnoreCase))
                    cleanId = cleanId[4..];
                if (cleanId.StartsWith("#"))
                    cleanId = cleanId[1..];
                if (int.TryParse(cleanId, out int id))
                    query = query.Where(o => o.OrderId == id);
                else
                    query = query.Where(o => o.CustomerPhone.Contains(cleanId));
            }
            else if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var phone = phoneNumber.Trim();
                query = query.Where(o => o.CustomerPhone.Contains(phone));
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Take(3)
                .Select(o => new
                {
                    orderCode = $"ORD-{o.OrderId}",
                    customerName = o.CustomerName,
                    orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    totalPrice = o.TotalPrice,
                    status = o.Status,
                    itemCount = o.OrderDetails.Count
                })
                .ToListAsync();

            if (orders.Count == 0)
                return JsonSerializer.Serialize(new { found = false, message = "Không tìm thấy đơn hàng nào khớp với thông tin cung cấp." });

            return JsonSerializer.Serialize(new { found = true, orders });
        }

        private string FormatProducts(IReadOnlyList<ProductFactDto> products)
        {
            _toolContext.RecordProducts(products);

            if (products.Count == 0)
                return JsonSerializer.Serialize(new { found = false, message = "Không có sản phẩm phù hợp trong dữ liệu hiện tại." });

            var response = products.Select(product => new
            {
                id = product.ProductId,
                name = product.Name,
                price = product.Price,
                currency = "VND",
                category = product.Category,
                description = TrimDescription(product.Description),
                soldQuantity = product.SoldQuantity
            });

            return JsonSerializer.Serialize(new { found = true, products = response });
        }

        private static string? TrimDescription(string? description) =>
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Length <= 280 ? description : description[..280] + "…";
    }
}
