using System.Text;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services.AI
{
    public class AdminAdvancedAnalyticsService : IAdminAdvancedAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminAdvancedAnalyticsService> _logger;

        public AdminAdvancedAnalyticsService(ApplicationDbContext context, ILogger<AdminAdvancedAnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RevenueTrendAnalysisDto> AnalyzeRevenueTrendAsync(string periodType = "week", CancellationToken ct = default)
        {
            var now = DateTime.Now;
            DateTime currentStart, previousStart, previousEnd;
            string periodLabel;

            if (periodType.Equals("month", StringComparison.OrdinalIgnoreCase))
            {
                currentStart = new DateTime(now.Year, now.Month, 1);
                previousStart = currentStart.AddMonths(-1);
                previousEnd = currentStart;
                periodLabel = $"Tháng {now.Month}/{now.Year} vs Tháng {previousStart.Month}/{previousStart.Year}";
            }
            else
            {
                // Default: week (Last 7 days vs previous 7 days)
                currentStart = now.Date.AddDays(-7);
                previousEnd = currentStart;
                previousStart = currentStart.AddDays(-7);
                periodLabel = "7 ngày qua vs 7 ngày trước đó";
            }

            var currentOrders = await _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt >= currentStart && o.ConfirmedAt <= now && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .ToListAsync(ct);

            var previousOrders = await _context.Orders.AsNoTracking()
                .Where(o => o.ConfirmedAt >= previousStart && o.ConfirmedAt < previousEnd && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .ToListAsync(ct);

            var currentRev = currentOrders.Sum(o => o.TotalPrice);
            var prevRev = previousOrders.Sum(o => o.TotalPrice);
            var currentCount = currentOrders.Count;
            var prevCount = previousOrders.Count;

            var currentAov = currentCount > 0 ? currentRev / currentCount : 0;
            var prevAov = prevCount > 0 ? prevRev / prevCount : 0;
            var growthPct = prevRev > 0 ? ((currentRev - prevRev) / prevRev) * 100 : (currentRev > 0 ? 100 : 0);

            var trendDirection = growthPct > 0.5m ? "Tăng" : (growthPct < -0.5m ? "Giảm" : "Ổn định");

            // Analyze product level contribution (Why did it increase/decrease?)
            var currentProductSales = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt >= currentStart && o.ConfirmedAt <= now && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by new { p.ProductId, p.ProductName } into g
                select new { g.Key.ProductId, g.Key.ProductName, Revenue = g.Sum(x => x.Quantity * x.Price), Quantity = g.Sum(x => x.Quantity) }
            ).ToListAsync(ct);

            var prevProductSales = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt >= previousStart && o.ConfirmedAt < previousEnd && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by new { p.ProductId, p.ProductName } into g
                select new { g.Key.ProductId, g.Key.ProductName, Revenue = g.Sum(x => x.Quantity * x.Price), Quantity = g.Sum(x => x.Quantity) }
            ).ToListAsync(ct);

            var productDiffs = new List<(string ProductName, decimal DiffRevenue, int CurrentQty, int PrevQty)>();
            var allProductIds = currentProductSales.Select(x => x.ProductId).Union(prevProductSales.Select(x => x.ProductId)).Distinct();

            foreach (var pid in allProductIds)
            {
                var cur = currentProductSales.FirstOrDefault(x => x.ProductId == pid);
                var prev = prevProductSales.FirstOrDefault(x => x.ProductId == pid);
                var name = cur?.ProductName ?? prev?.ProductName ?? "Bánh ngọt";
                var curRev = cur?.Revenue ?? 0;
                var prevRevVal = prev?.Revenue ?? 0;
                productDiffs.Add((name, curRev - prevRevVal, cur?.Quantity ?? 0, prev?.Quantity ?? 0));
            }

            var topGrowth = productDiffs.OrderByDescending(x => x.DiffRevenue).Take(3).Where(x => x.DiffRevenue > 0).ToList();
            var topDecline = productDiffs.OrderBy(x => x.DiffRevenue).Take(3).Where(x => x.DiffRevenue < 0).ToList();

            var reasons = new List<string>();
            if (growthPct > 0)
            {
                reasons.Add($"Doanh thu tăng {growthPct:N1}% chủ yếu nhờ lượng đơn hàng đạt {currentCount} đơn (so với {prevCount} đơn kỳ trước).");
                if (topGrowth.Any())
                {
                    var names = string.Join(", ", topGrowth.Select(g => $"{g.ProductName} (+{g.DiffRevenue:N0}đ)"));
                    reasons.Add($"Các dòng bánh dẫn dắt đà tăng trưởng mạnh nhất: {names}.");
                }
            }
            else if (growthPct < 0)
            {
                reasons.Add($"Doanh thu giảm {Math.Abs(growthPct):N1}% do số đơn hàng là {currentCount} đơn (kỳ trước {prevCount} đơn).");
                if (topDecline.Any())
                {
                    var names = string.Join(", ", topDecline.Select(d => $"{d.ProductName} ({d.DiffRevenue:N0}đ)"));
                    reasons.Add($"Các dòng bánh suy giảm doanh thu rõ rệt: {names}.");
                }
            }
            else
            {
                reasons.Add($"Doanh thu duy trì ngang mức kỳ trước ({currentRev:N0}đ vs {prevRev:N0}đ).");
            }

            return new RevenueTrendAnalysisDto
            {
                PeriodLabel = periodLabel,
                CurrentPeriodRevenue = currentRev,
                PreviousPeriodRevenue = prevRev,
                GrowthPercentage = growthPct,
                CurrentOrders = currentCount,
                PreviousOrders = prevCount,
                CurrentAverageOrderValue = currentAov,
                PreviousAverageOrderValue = prevAov,
                TrendDirection = trendDirection,
                KeyReasons = reasons,
                TopGrowthProducts = topGrowth.Select(g => $"{g.ProductName} (+{g.DiffRevenue:N0}đ)").ToList(),
                TopDeclineProducts = topDecline.Select(d => $"{d.ProductName} ({d.DiffRevenue:N0}đ)").ToList()
            };
        }

        public async Task<List<TopSellingByPeriodDto>> GetTopSellingByPeriodAsync(string periodType = "month", int limit = 5, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            DateTime startDate;
            string periodLabel;

            if (periodType.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                startDate = now.Date;
                periodLabel = "Hôm nay";
            }
            else if (periodType.Equals("week", StringComparison.OrdinalIgnoreCase))
            {
                startDate = now.Date.AddDays(-7);
                periodLabel = "7 ngày qua";
            }
            else if (periodType.Equals("year", StringComparison.OrdinalIgnoreCase))
            {
                startDate = new DateTime(now.Year, 1, 1);
                periodLabel = $"Năm {now.Year}";
            }
            else
            {
                // Default: this month
                startDate = new DateTime(now.Year, now.Month, 1);
                periodLabel = $"Tháng {now.Month}/{now.Year}";
            }

            var query = from od in _context.OrderDetails.AsNoTracking()
                        join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                        join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                        join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId into catGroup
                        from c in catGroup.DefaultIfEmpty()
                        where o.ConfirmedAt >= startDate && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                        group od by new { p.ProductId, p.ProductName, CategoryName = c != null ? c.CategoryName : "Khác" } into g
                        select new TopSellingByPeriodDto
                        {
                            ProductId = g.Key.ProductId,
                            ProductName = g.Key.ProductName,
                            CategoryName = g.Key.CategoryName,
                            TotalQuantitySold = g.Sum(x => x.Quantity),
                            TotalRevenueGenerated = g.Sum(x => x.Quantity * x.Price),
                            PeriodLabel = periodLabel
                        };

            var list = await query.OrderByDescending(x => x.TotalQuantitySold).ThenByDescending(x => x.TotalRevenueGenerated)
                .Take(limit)
                .ToListAsync(ct);

            return list;
        }

        public async Task<OrderChannelBreakdownDto> GetOrderChannelBreakdownAsync(CancellationToken ct = default)
        {
            var orders = await _context.Orders.AsNoTracking()
                .Where(o => OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .ToListAsync(ct);

            var totalOrders = orders.Count;
            var totalRev = orders.Sum(o => o.TotalPrice);

            var memberOrders = orders.Where(o => !o.IsGuest && !string.IsNullOrWhiteSpace(o.UserId)).ToList();
            var guestOrders = orders.Where(o => o.IsGuest || string.IsNullOrWhiteSpace(o.UserId)).ToList();

            var couponOrders = orders.Where(o => !string.IsNullOrWhiteSpace(o.CouponCode) || o.DiscountAmount > 0).ToList();

            var provinceBreakdown = orders
                .Where(o => !string.IsNullOrWhiteSpace(o.Province))
                .GroupBy(o => o.Province.Trim())
                .Select(g => (ProvinceOrCity: g.Key, OrdersCount: g.Count(), Revenue: g.Sum(x => x.TotalPrice)))
                .OrderByDescending(x => x.OrdersCount)
                .Take(5)
                .ToList();

            var summary = new StringBuilder();
            if (totalOrders > 0)
            {
                var memberPct = (decimal)memberOrders.Count / totalOrders * 100;
                var guestPct = (decimal)guestOrders.Count / totalOrders * 100;
                summary.Append($"Trong tổng số {totalOrders} đơn hàng thành công, khách vãng lai (Guest) chiếm {guestPct:N1}% ({guestOrders.Count} đơn), khách thành viên đăng ký chiếm {memberPct:N1}% ({memberOrders.Count} đơn). ");
                if (provinceBreakdown.Any())
                {
                    summary.Append($"Khu vực đặt hàng nhiều nhất là {provinceBreakdown[0].ProvinceOrCity} với {provinceBreakdown[0].OrdersCount} đơn ({provinceBreakdown[0].Revenue:N0}đ). ");
                }
                if (couponOrders.Any())
                {
                    summary.Append($"Có {couponOrders.Count} đơn áp dụng mã giảm giá với tổng số tiền chiết khấu là {couponOrders.Sum(x => x.DiscountAmount):N0}đ.");
                }
            }
            else
            {
                summary.Append("Chưa có đủ dữ liệu đơn hàng thành công để phân tích kênh.");
            }

            return new OrderChannelBreakdownDto
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRev,
                MemberOrdersCount = memberOrders.Count,
                MemberOrdersRevenue = memberOrders.Sum(o => o.TotalPrice),
                GuestOrdersCount = guestOrders.Count,
                GuestOrdersRevenue = guestOrders.Sum(o => o.TotalPrice),
                CouponAppliedOrdersCount = couponOrders.Count,
                CouponTotalDiscount = couponOrders.Sum(o => o.DiscountAmount),
                TopProvinces = provinceBreakdown,
                PrimaryChannelSummary = summary.ToString()
            };
        }

        public async Task<DynamicAnalyticsQueryResultDto> ExecuteDynamicAnalyticsQueryAsync(string naturalLanguageQuery, CancellationToken ct = default)
        {
            var q = naturalLanguageQuery.ToLower().Trim();
            var rows = new List<Dictionary<string, object>>();
            var conclusion = string.Empty;
            var sqlSummary = string.Empty;
            var intent = "Dynamic_SQL_Query";

            // Dynamic safe query filtering based on natural language keywords (Text-to-SQL behavior)
            if (q.Contains("giảm giá") || q.Contains("khuyến mãi") || q.Contains("coupon"))
            {
                intent = "SQL_Select_Coupons_And_Discounts";
                sqlSummary = "SELECT CouponCode, COUNT(OrderId) AS TotalOrders, SUM(DiscountAmount) AS TotalDiscount FROM Orders WHERE DiscountAmount > 0 GROUP BY CouponCode";
                
                var couponStats = await _context.Orders.AsNoTracking()
                    .Where(o => o.DiscountAmount > 0 && !string.IsNullOrWhiteSpace(o.CouponCode))
                    .GroupBy(o => o.CouponCode!)
                    .Select(g => new { CouponCode = g.Key, Orders = g.Count(), TotalDiscount = g.Sum(x => x.DiscountAmount) })
                    .ToListAsync(ct);

                foreach (var item in couponStats)
                {
                    rows.Add(new Dictionary<string, object>
                    {
                        ["Mã Coupon"] = item.CouponCode,
                        ["Số Đơn Áp Dụng"] = item.Orders,
                        ["Tổng Chiết Khấu"] = $"{item.TotalDiscount:N0} VNĐ"
                    });
                }
                conclusion = couponStats.Any()
                    ? $"Tìm thấy {couponStats.Count} mã giảm giá đã được sử dụng, trong đó mã [{couponStats.OrderByDescending(x => x.Orders).First().CouponCode}] được dùng nhiều nhất với {couponStats.OrderByDescending(x => x.Orders).First().Orders} đơn."
                    : "Hiện chưa có đơn hàng nào áp dụng mã coupon giảm giá trong hệ thống.";
            }
            else if (q.Contains("tồn kho") || q.Contains("sắp hết") || q.Contains("nguyên liệu"))
            {
                intent = "SQL_Select_Low_Inventory";
                sqlSummary = "SELECT Name, Measurement, Quantity FROM Ingredients WHERE Quantity <= 5";
                
                var lowStock = await _context.Ingredients.AsNoTracking()
                    .Where(i => i.Quantity <= 5)
                    .Select(i => new { i.Name, i.Measurement, i.Quantity })
                    .ToListAsync(ct);

                foreach (var item in lowStock)
                {
                    rows.Add(new Dictionary<string, object>
                    {
                        ["Nguyên liệu"] = item.Name,
                        ["Tồn hiện tại"] = $"{item.Quantity} {item.Measurement}",
                        ["Đánh giá"] = item.Quantity <= 2 ? "Rất nguy cấp (< 2)" : "Sắp hết (<= 5)"
                    });
                }
                conclusion = lowStock.Any()
                    ? $"Cảnh báo: Có {lowStock.Count} nguyên liệu đang chạm hoặc dưới ngưỡng báo động tồn kho cần bổ sung gấp."
                    : "Tồn kho tất cả nguyên liệu hiện đang ở mức an toàn, không có nguyên liệu nào dưới ngưỡng cảnh báo.";
            }
            else if (q.Contains("chờ") || q.Contains("pending") || q.Contains("đơn mới"))
            {
                intent = "SQL_Select_Pending_Orders_Details";
                sqlSummary = "SELECT OrderId, CustomerName, CustomerPhone, TotalPrice, OrderDate FROM Orders WHERE Status = 'Pending' ORDER BY OrderDate DESC";
                
                var pendings = await _context.Orders.AsNoTracking()
                    .Where(o => o.Status == "Pending")
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new { o.OrderId, o.CustomerName, o.CustomerPhone, o.TotalPrice, o.OrderDate })
                    .ToListAsync(ct);

                foreach (var item in pendings)
                {
                    rows.Add(new Dictionary<string, object>
                    {
                        ["Mã Đơn"] = $"#{item.OrderId}",
                        ["Khách Hàng"] = item.CustomerName,
                        ["Số Điện Thoại"] = item.CustomerPhone,
                        ["Tổng Tiền"] = $"{item.TotalPrice:N0} VNĐ",
                        ["Thời Gian Đặt"] = item.OrderDate.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                conclusion = pendings.Any()
                    ? $"Hệ thống đang có {pendings.Count} đơn hàng ở trạng thái chờ xử lý (Pending) cần Admin/Nhân viên xác nhận sớm."
                    : "Hiện tại không có đơn hàng nào ở trạng thái chờ xử lý (Pending).";
            }
            else
            {
                // General Dynamic Product & Sales Aggregation Query
                intent = "SQL_Select_General_Sales_Aggregation";
                sqlSummary = "SELECT P.ProductName, C.CategoryName, P.Price FROM Products P LEFT JOIN Categories C ON P.CategoryId = C.CategoryId ORDER BY P.ProductId DESC";
                
                var products = await _context.Products.AsNoTracking().Include(p => p.Category)
                    .OrderByDescending(p => p.ProductId)
                    .Take(8)
                    .ToListAsync(ct);

                foreach (var p in products)
                {
                    rows.Add(new Dictionary<string, object>
                    {
                        ["ID"] = p.ProductId,
                        ["Tên Bánh"] = p.ProductName,
                        ["Danh Mục"] = p.Category?.CategoryName ?? "Khác",
                        ["Đơn Giá"] = $"{p.Price:N0} VNĐ"
                    });
                }
                conclusion = $"Truy vấn tổng hợp: hiển thị {products.Count} sản phẩm nổi bật mới cập nhật trong CSDL cùng trạng thái kinh doanh.";
            }

            return new DynamicAnalyticsQueryResultDto
            {
                QueryIntent = intent,
                ExecutedSqlOrSummary = sqlSummary,
                Rows = rows,
                NaturalLanguageConclusion = conclusion
            };
        }
    }
}
