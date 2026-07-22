using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SweetCakeShop.Models;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI.Rag
{
    public class ConsultantResponseService : IConsultantResponseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConsultantResponseService> _logger;

        public ConsultantResponseService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ConsultantResponseService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> GenerateAsync(
            AiChatMode mode,
            string userMessage,
            RagKnowledgeDocument knowledge,
            IReadOnlyList<ChatMessage> history,
            string languageCode,
            ConversationSessionState session,
            CancellationToken cancellationToken = default)
        {
            var system = BuildConsultantSystemPrompt(mode);
            var user = BuildUserPrompt(userMessage, knowledge, session, languageCode, mode);

            var geminiKey = GetApiKey("Gemini:ApiKey", "GEMINI_API_KEY");
            if (geminiKey != null)
            {
                var r = await TryGeminiAsync(geminiKey, system, history, user, mode, cancellationToken);
                if (!string.IsNullOrWhiteSpace(r)) return mode == AiChatMode.Admin ? r!.Trim() : TrimToMaxSentences(r!, 3);
            }

            var openAiKey = GetApiKey("OpenAI:ApiKey", "OPENAI_API_KEY");
            if (openAiKey != null)
            {
                var r = await TryOpenAiAsync(openAiKey, system, history, user, mode, cancellationToken);
                if (!string.IsNullOrWhiteSpace(r)) return mode == AiChatMode.Admin ? r!.Trim() : TrimToMaxSentences(r!, 3);
            }

            return null;
        }

        private static string BuildConsultantSystemPrompt(AiChatMode mode)
        {
            if (mode == AiChatMode.Admin)
            {
                return """
                    Bạn là Trợ lý AI Quản trị Phân tích Kinh doanh nâng cao (Executive AI Analyst) của SweetCakeShop — thông minh, sắc bén, mạch lạc.
                    NHIỆM VỤ: Phân tích số liệu từ [Dữ liệu cửa hàng] (doanh thu, xu hướng tăng/giảm vì sao, top bán chạy, kênh bán hàng, Text-to-SQL động).
                    QUY TẮC BẮT BUỘC:
                    - CHỈ dùng số liệu thực tế trong [Dữ liệu cửa hàng], tuyệt đối không bịa số liệu.
                    - Trình bày mạch lạc bằng các ý gạch đầu dòng, nêu rõ nguyên nhân ("Vì sao doanh thu tăng/giảm?"), có số liệu cụ thể và tỷ lệ %.
                    - Nếu có bảng biểu hay danh sách từ Text-to-SQL động, hãy hiển thị rõ ràng, dễ đọc cho Ban Quản trị.
                    - Dùng icon 📈 📊 🎂 💰 để báo cáo trực quan, chuyên nghiệp.
                    """;
            }

            return """
                Bạn là nhân viên tư vấn bán bánh vô cùng thân thiện, chuyên nghiệp và ngắn gọn của cửa hàng SweetCakeShop.

                NHIỆM VỤ:
                1. Tư vấn bánh phù hợp (sở thích, số người, ngân sách) theo DỮ LIỆU.
                2. Khi khách muốn đặt: gợi ý thu thập Tên, SĐT, Địa chỉ, Ngày giờ nhận, Nội dung ghi bánh (hướng dẫn ngắn).

                QUY TẮC BẮT BUỘC:
                - CHỈ trả lời từ [Dữ liệu cửa hàng]. KHÔNG bịa bánh, giá, chính sách.
                - Không có dữ liệu: "Dạ hiện tại tiệm em chưa có loại bánh này/chưa có thông tin này, anh/chị đợi em một chút để em báo nhân viên tiệm hỗ trợ mình ngay nhé ạ!" — không nói "Tôi không biết".
                - NGẮN GỌN: tối đa 3 câu. Không spam, không lặp, không dài dòng.
                - Dùng icon 🍰 🎂 🍩 phù hợp (1-2 cái).
                - Xưng hô: Dạ, anh/chị, em.
                """;
        }

        private static string BuildUserPrompt(
            string userMessage,
            RagKnowledgeDocument knowledge,
            ConversationSessionState session,
            string languageCode,
            AiChatMode mode)
        {
            var focus = session.Focus?.ProductName != null
                ? $"\nSản phẩm đang nói: {session.Focus.ProductName}"
                : "";
            var lengthInstruction = mode == AiChatMode.Admin
                ? "Trả lời rõ ràng, chi tiết và đầy đủ các ý phân tích kinh doanh, có số liệu và lý do."
                : "Trả lời ĐÚNG câu hỏi, tối đa 3 câu.";

            return $"""
                Ngôn ngữ trả lời: {languageCode}
                Loại truy vấn dữ liệu: {knowledge.PrimaryFunction}
                {focus}

                [Dữ liệu cửa hàng]
                {knowledge.StoreDataBlock}

                Câu hỏi: {userMessage}

                {lengthInstruction}
                """;
        }

        private static string TrimToMaxSentences(string text, int max)
        {
            var cleaned = text.Trim();
            var parts = Regex.Split(cleaned, @"(?<=[.!?…])\s+");
            if (parts.Length <= max) return cleaned;
            return string.Join(" ", parts.Take(max));
        }

        private async Task<string?> TryGeminiAsync(
            string apiKey,
            string system,
            IReadOnlyList<ChatMessage> history,
            string userPrompt,
            AiChatMode mode,
            CancellationToken ct)
        {
            try
            {
                var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var contents = new List<object>();
                foreach (var m in history.TakeLast(4))
                {
                    var role = m.Role == "assistant" ? "model" : "user";
                    contents.Add(new { role, parts = new[] { new { text = m.Content } } });
                }
                contents.Add(new { role = "user", parts = new[] { new { text = userPrompt } } });

                var payload = new
                {
                    systemInstruction = new { parts = new[] { new { text = system } } },
                    contents,
                    generationConfig = new { temperature = GetConsultantTemperature(), maxOutputTokens = GetMaxTokens(mode) }
                };

                var client = _httpClientFactory.CreateClient("Gemini");
                using var res = await client.PostAsync(url,
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                if (!res.IsSuccessStatusCode) return null;

                var body = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("candidates")[0]
                    .GetProperty("content").GetProperty("parts")[0]
                    .GetProperty("text").GetString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini consultant failed");
                return null;
            }
        }

        private async Task<string?> TryOpenAiAsync(
            string apiKey,
            string system,
            IReadOnlyList<ChatMessage> history,
            string userPrompt,
            AiChatMode mode,
            CancellationToken ct)
        {
            try
            {
                var messages = new List<object> { new { role = "system", content = system } };
                foreach (var m in history.TakeLast(4))
                    messages.Add(new { role = m.Role == "assistant" ? "assistant" : "user", content = m.Content });
                messages.Add(new { role = "user", content = userPrompt });

                var client = _httpClientFactory.CreateClient("OpenAI");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini",
                    temperature = GetConsultantTemperature(),
                    max_tokens = GetMaxTokens(mode),
                    messages
                };

                using var res = await client.PostAsync(
                    _configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
                if (!res.IsSuccessStatusCode) return null;

                var body = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content").GetString()?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI consultant failed");
                return null;
            }
        }

        private string? GetApiKey(string configKey, string envKey)
        {
            var key = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        /// <summary>Thấp (0.1–0.2) = ít bịa, ít spam. Mặc định 0.15.</summary>
        private float GetConsultantTemperature()
        {
            var v = _configuration["AiChat:ConsultantTemperature"];
            return float.TryParse(v, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var t)
                ? Math.Clamp(t, 0f, 0.4f)
                : 0.15f;
        }

        private int GetMaxTokens(AiChatMode mode)
        {
            if (mode == AiChatMode.Admin) return 1000;
            var v = _configuration["AiChat:MaxOutputTokens"];
            return int.TryParse(v, out var n) ? Math.Clamp(n, 80, 500) : 280;
        }
    }
}
