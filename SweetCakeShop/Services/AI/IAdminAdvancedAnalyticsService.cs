using SweetCakeShop.Models;

namespace SweetCakeShop.Services.AI
{
    public class RevenueTrendAnalysisDto
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public decimal CurrentPeriodRevenue { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public decimal GrowthPercentage { get; set; }
        public int CurrentOrders { get; set; }
        public int PreviousOrders { get; set; }
        public decimal CurrentAverageOrderValue { get; set; }
        public decimal PreviousAverageOrderValue { get; set; }
        public string TrendDirection { get; set; } = string.Empty;
        public List<string> KeyReasons { get; set; } = new();
        public List<string> TopGrowthProducts { get; set; } = new();
        public List<string> TopDeclineProducts { get; set; } = new();
    }

    public class TopSellingByPeriodDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
    }

    public class OrderChannelBreakdownDto
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int MemberOrdersCount { get; set; }
        public decimal MemberOrdersRevenue { get; set; }
        public int GuestOrdersCount { get; set; }
        public decimal GuestOrdersRevenue { get; set; }
        public int CouponAppliedOrdersCount { get; set; }
        public decimal CouponTotalDiscount { get; set; }
        public List<(string ProvinceOrCity, int OrdersCount, decimal Revenue)> TopProvinces { get; set; } = new();
        public string PrimaryChannelSummary { get; set; } = string.Empty;
    }

    public class DynamicAnalyticsQueryResultDto
    {
        public string QueryIntent { get; set; } = string.Empty;
        public string ExecutedSqlOrSummary { get; set; } = string.Empty;
        public List<Dictionary<string, object>> Rows { get; set; } = new();
        public string NaturalLanguageConclusion { get; set; } = string.Empty;
    }

    public interface IAdminAdvancedAnalyticsService
    {
        Task<RevenueTrendAnalysisDto> AnalyzeRevenueTrendAsync(string periodType = "week", CancellationToken ct = default);
        Task<List<TopSellingByPeriodDto>> GetTopSellingByPeriodAsync(string periodType = "month", int limit = 5, CancellationToken ct = default);
        Task<OrderChannelBreakdownDto> GetOrderChannelBreakdownAsync(CancellationToken ct = default);
        Task<DynamicAnalyticsQueryResultDto> ExecuteDynamicAnalyticsQueryAsync(string naturalLanguageQuery, CancellationToken ct = default);
    }
}
