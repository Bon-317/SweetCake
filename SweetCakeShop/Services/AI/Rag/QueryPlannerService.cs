using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI.Rag
{
    public class QueryPlannerService : IQueryPlannerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QueryPlannerService> _logger;

        public QueryPlannerService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<QueryPlannerService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiFunctionCall> PlanSingleFunctionAsync(
            AiChatMode mode,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState session,
            CancellationToken cancellationToken = default)
        {
            var isAdmin = mode == AiChatMode.Admin;
            var allowed = isAdmin ? AiToolDefinitions.AdminFunctionNames : AiToolDefinitions.CustomerFunctionNames;

            if (isAdmin)
            {
                var fastMap = SemanticFunctionMapper.Map(userMessage, session, true);
                if (fastMap.Name is "AnalyzeRevenueTrend" or "GetTopSellingByPeriod" or "GetOrderChannelBreakdown" or "ExecuteDynamicAnalyticsQuery")
                {
                    return fastMap;
                }
            }

            var llmResult = await PlanWithLlmJsonAsync(isAdmin, userMessage, history, session, allowed, cancellationToken);
            if (llmResult != null && IsValidFunction(llmResult.Name, allowed) && llmResult.Name != "GeneralConsultation")
                return llmResult;

            var mapped = SemanticFunctionMapper.Map(userMessage, session, isAdmin);
            if (IsValidFunction(mapped.Name, allowed))
                return mapped;

            return new AiFunctionCall
            {
                Name = isAdmin ? "ExecuteDynamicAnalyticsQuery" : "GetProductList",
                Arguments = isAdmin ? new() { ["query"] = userMessage } : new()
            };
        }

        private async Task<AiFunctionCall?> PlanWithLlmJsonAsync(
            bool isAdmin,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState session,
            string[] allowed,
            CancellationToken ct)
        {
            var openAiKey = GetApiKey("OpenAI:ApiKey", "OPENAI_API_KEY");
            if (openAiKey != null)
            {
                try
                {
                    var prompt = BuildPlannerPrompt(isAdmin, userMessage, history, session, allowed);
                    return await PlanOpenAiAsync(openAiKey, prompt, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI planner failed");
                }
            }

            var geminiKey = GetApiKey("Gemini:ApiKey", "GEMINI_API_KEY");
            if (geminiKey != null)
            {
                try
                {
                    var prompt = BuildPlannerPrompt(isAdmin, userMessage, history, session, allowed);
                    return await PlanGeminiAsync(geminiKey, prompt, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini planner failed");
                }
            }

            return null;
        }

        private static string BuildPlannerPrompt(
            bool isAdmin,
            string userMessage,
            IReadOnlyList<ChatMessage> history,
            ConversationSessionState session,
            string[] allowed)
        {
            var sb = new StringBuilder();
            sb.AppendLine(isAdmin ? "Role: Admin staff analytics." : "Role: Customer bakery consultant data lookup.");
            if (session.Focus?.ProductName != null)
                sb.AppendLine($"Focus product: {session.Focus.ProductName}");
            foreach (var m in history.TakeLast(4))
                sb.AppendLine($"{m.Role}: {m.Content}");
            sb.AppendLine($"User question: {userMessage}");
            sb.AppendLine("Allowed functions: " + string.Join(", ", allowed));
            if (isAdmin)
            {
                sb.AppendLine("""
                    Return JSON ONLY: {"function":"FunctionName","arguments":{}}
                    Admin selection rules:
                    - AnalyzeRevenueTrend: revenue trend comparison, growth/decline percentage, why ("vì sao tăng", "vì sao giảm", "so sánh doanh thu", "xu hướng"). arguments: {"periodType": "week" or "month"}.
                    - GetTopSellingByPeriod: top selling products by period ("top bán chạy hôm nay", "bán chạy nhất tháng này", "7 ngày qua"). arguments: {"periodType": "today"/"week"/"month"/"year", "limit": 5}.
                    - GetOrderChannelBreakdown: sales channels, member vs guest orders, regional province distribution, coupon summary ("kênh bán hàng", "thành viên vs vãng lai", "khu vực đặt hàng"). arguments: {}.
                    - ExecuteDynamicAnalyticsQuery: custom SQL queries, specific coupon usage list ("mã giảm giá coupon dùng nhiều nhất"), low stock thresholds ("nguyên liệu sắp hết", "tồn kho"), pending orders details ("đơn hàng chờ xử lý", "sđt khách"). arguments: {"query": "user question string"}.
                    - GetTodayRevenue / GetWeeklyRevenue / GetMonthlyRevenue / GetYearlyRevenue: simple total revenue amount ("doanh thu hôm nay bao nhiêu").
                    - GetCakesSoldToday: simple number of cakes sold today.
                    - GetTopCustomers: top spending customers ("khách hàng VIP").
                    - GetIngredientsOverview: general ingredients list.
                    - GetProductList / SearchProducts: general catalog lookup.
                    """);
            }
            else
            {
                sb.AppendLine("""
                    Return JSON ONLY: {"function":"FunctionName","arguments":{}}
                    Rules:
                    - Pick the ONE best function for the question semantics (any language).
                    - cheapest/rẻ nhất -> GetCheapestProduct. expensive/đắt nhất -> GetHighestPriceProduct.
                    - best seller/bán chạy -> GetTopSellingProduct. chocolate/socola -> SearchProducts query socola.
                    - list cakes/danh sách/còn bao nhiêu sản phẩm -> GetProductList. kem -> SearchProducts query kem. mì/bread -> SearchProducts query mì.
                    - birthday/sinh nhật/girlfriend -> RecommendProducts with occasion.
                    - NEVER use GeneralConsultation if a specific function fits.
                    """);
            }
            return sb.ToString();
        }

        private async Task<AiFunctionCall?> PlanOpenAiAsync(string apiKey, string prompt, CancellationToken ct)
        {
            var endpoint = _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model,
                temperature = 0,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new { role = "system", content = "You classify bakery shop questions into exactly one database function. JSON only." },
                    new { role = "user", content = prompt }
                }
            };

            using var response = await client.PostAsync(
                endpoint,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return ParseFunctionJson(content);
        }

        private async Task<AiFunctionCall?> PlanGeminiAsync(string apiKey, string prompt, CancellationToken ct)
        {
            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt + "\nOutput strict valid JSON only without markdown codeblocks or extra text." }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0f,
                    responseMimeType = "application/json"
                }
            };

            var client = _httpClientFactory.CreateClient("Gemini");
            using var response = await client.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content").GetProperty("parts")[0]
                .GetProperty("text").GetString()?.Trim();

            if (text?.StartsWith("```json", StringComparison.OrdinalIgnoreCase) == true)
            {
                text = text.Substring(7);
                if (text.EndsWith("```")) text = text.Substring(0, text.Length - 3);
                text = text.Trim();
            }
            else if (text?.StartsWith("```") == true)
            {
                text = text.Substring(3);
                if (text.EndsWith("```")) text = text.Substring(0, text.Length - 3);
                text = text.Trim();
            }

            return ParseFunctionJson(text);
        }

        private static AiFunctionCall? ParseFunctionJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var name = root.TryGetProperty("function", out var fn) ? fn.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) return null;

                var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in argsEl.EnumerateObject())
                        args[p.Name] = p.Value.Clone();
                }
                return new AiFunctionCall { Name = name, Arguments = args };
            }
            catch
            {
                return null;
            }
        }

        private static bool IsValidFunction(string name, string[] allowed) =>
            allowed.Contains(name, StringComparer.OrdinalIgnoreCase);

        private string? GetApiKey(string configKey, string envKey)
        {
            var key = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }
    }

    internal static class SemanticFunctionMapper
    {
        public static AiFunctionCall Map(string message, ConversationSessionState session, bool isAdmin)
        {
            var t = Regex.Replace(message.ToLowerInvariant().Replace('đ', 'd'), @"\s+", " ");

            if (isAdmin)
            {
                if (Regex.IsMatch(t, @"tăng hay giảm|vì sao|vi sao|xu hướng|xu huong|so sánh.*doanh thu|trend|lý do|chênh lệch"))
                {
                    var period = Regex.IsMatch(t, @"tháng|thang|month") ? "month" : "week";
                    return new AiFunctionCall { Name = "AnalyzeRevenueTrend", Arguments = new() { ["periodType"] = period } };
                }
                if (Regex.IsMatch(t, @"sql|truy vấn|truy van|thống kê động|query|db|mã giảm giá|ma giam gia|coupon|ngưỡng|nguong|báo động|bao dong|sắp hết|sap het|chờ|cho |pending|đơn mới|don moi|tình trạng|tinh trang"))
                    return new AiFunctionCall { Name = "ExecuteDynamicAnalyticsQuery", Arguments = new() { ["query"] = message } };
                if (Regex.IsMatch(t, @"kênh|kenh|channel|vãng lai|vang lai|thành viên|thanh vien|khu vực|khu vuc|tỉnh|tinh|thành phố|thanh pho"))
                    return Fn("GetOrderChannelBreakdown");
                if (Regex.IsMatch(t, @"bán chạy.*(tháng|tuần|hôm nay|năm|kỳ|7 ngày)|top.*(tháng|tuần|hôm nay|năm|kỳ|7 ngày)|ban chay.*(thang|tuan|hom nay|nam|7 ngay)"))
                {
                    var period = Regex.IsMatch(t, @"hôm nay|nay|today") ? "today"
                        : Regex.IsMatch(t, @"tuần|tuan|week|7 ngày|7 ngay") ? "week"
                        : Regex.IsMatch(t, @"năm|nam|year") ? "year" : "month";
                    return new AiFunctionCall { Name = "GetTopSellingByPeriod", Arguments = new() { ["periodType"] = period, ["limit"] = 5 } };
                }
                if (Regex.IsMatch(t, @"bán.*(hôm nay|nay)|nay.*ban|cake.*sold|so.*banh.*hom nay"))
                    return new AiFunctionCall { Name = "GetCakesSoldToday", Arguments = new() };
                if (Regex.IsMatch(t, @"doanh thu|revenue|doanh so"))
                {
                    if (Regex.IsMatch(t, @"năm|nam|year")) return Fn("GetYearlyRevenue");
                    if (Regex.IsMatch(t, @"tháng|thang|month")) return Fn("GetMonthlyRevenue");
                    if (Regex.IsMatch(t, @"tuần|tuan|week")) return Fn("GetWeeklyRevenue");
                    return Fn("GetTodayRevenue");
                }
                if (Regex.IsMatch(t, @"bán chạy|ban chay|best sell|top sell")) return Fn("GetTopSellingProduct");
                if (Regex.IsMatch(t, @"ế nhất|e nhat|worst sell|bán kém|ban kem")) return Fn("GetWorstSellingProduct");
                if (Regex.IsMatch(t, @"khách.*(nhiều|mua)|top customer|vip")) return Fn("GetTopCustomers");
                if (Regex.IsMatch(t, @"nguyên liệu|nguyen lieu|ingredient|tồn kho|ton kho")) return Fn("GetIngredientsOverview");
                if (Regex.IsMatch(t, @"sản phẩm|san pham|bánh|banh|còn|con|tồn|ton|danh sách|danh sach|bao nhiêu|loại|loai"))
                    return Fn("GetProductList");
                if (Regex.IsMatch(t, @"trung bình|trung binh|average|tb")) return Fn("GetAverageOrderValue");
                if (Regex.IsMatch(t, @"tăng trưởng|tang truong|growth")) return Fn("GetRevenueGrowth");
                return new AiFunctionCall { Name = "ExecuteDynamicAnalyticsQuery", Arguments = new() { ["query"] = message } };
            }

            if (Regex.IsMatch(t, @"rẻ nhất|re nhat|cheapest|lowest price|ít tiền|it tien"))
                return Fn("GetCheapestProduct");
            if (Regex.IsMatch(t, @"đắt nhất|dat nhat|expensive|highest price|cao nhất|cao nhat"))
                return Fn("GetHighestPriceProduct");
            if (Regex.IsMatch(t, @"bán chạy|ban chay|best sell|top sell|phổ biến|pho bien"))
                return Fn("GetTopSellingProduct");
            if (Regex.IsMatch(t, @"socola|chocolate|kem|mì|mi |bread|list|danh sách|danh sach"))
            {
                var q = t.Contains("socola") || t.Contains("chocolate") ? "socola"
                    : t.Contains("kem") ? "kem"
                    : t.Contains("mì") || t.Contains("mi ") || t.Contains("bread") ? "mì"
                    : t.Contains("list") || t.Contains("danh sach") ? "banh" : message;
                return new AiFunctionCall { Name = "SearchProducts", Arguments = new() { ["query"] = q } };
            }
            if (Regex.IsMatch(t, @"sinh nhật|sinh nhat|birthday|bạn gái|ban gai|girlfriend|tặng|tang|quà|qua"))
                return new AiFunctionCall
                {
                    Name = "RecommendProducts",
                    Arguments = new() { ["occasion"] = t.Contains("birthday") || t.Contains("sinh") ? "birthday" : "gift", ["flavor"] = t.Contains("socola") ? "socola" : "" }
                };
            if (Regex.IsMatch(t, @"giá|gia|bao nhiêu|how much") && session.Focus?.ProductName != null)
                return Fn("GetProductDetails");
            if (Regex.IsMatch(t, @"giao hàng|giao hang|delivery|ship")) return Fn("GetDeliveryInformation");
            if (Regex.IsMatch(t, @"thanh toán|thanh toan|payment|cod")) return Fn("GetPaymentInformation");
            if (Regex.IsMatch(t, @"đặt hàng|dat hang|checkout|mua")) return Fn("GetCheckoutGuide");

            return Fn("GetProductList");
        }

        private static AiFunctionCall Fn(string name) => new() { Name = name, Arguments = new() };
    }
}
