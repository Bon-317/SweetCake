using System.Globalization;
using System.Text;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Chuẩn hóa văn bản tiếng Việt để tìm kiếm: loại bỏ dấu,
    /// chuyển thành chữ thường, và xóa khoảng trắng thừa để "Bánh kem" khớp với "banh kem".
    /// </summary>
    public interface IVietnameseNormalizerService
    {
        /// <summary>Chuẩn hóa văn bản: bỏ dấu, chữ thường, xóa khoảng trắng thừa.</summary>
        string Normalize(string? input);

        /// <summary>Kiểm tra xem <paramref name="source"/> có chứa <paramref name="search"/> sau khi chuẩn hóa không.</summary>
        bool FuzzyContains(string? source, string? search);

        /// <summary>
        /// Tính toán điểm liên quan (0.0–1.0) để xem <paramref name="candidate"/>
        /// khớp với <paramref name="query"/> như thế nào sau khi chuẩn hóa.
        /// </summary>
        double Score(string? candidate, string? query);
    }

    public class VietnameseNormalizerService : IVietnameseNormalizerService
    {
        // Thay thế các ký tự đặc biệt của tiếng Việt mà phân tách Unicode
        // không xử lý đúng (đ/Đ → d/D).
        private static readonly Dictionary<char, char> SpecialReplacements = new()
        {
            { 'đ', 'd' }, { 'Đ', 'd' }
        };

        private static readonly HashSet<string> BakeryStopWords = new()
        {
            "banh", "kem", "mi", "quy", "ngot", "hop", "loai", "chiec", "cai", "vi"
        };

        public string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);

            foreach (var ch in input)
            {
                if (SpecialReplacements.TryGetValue(ch, out var replacement))
                {
                    sb.Append(replacement);
                    continue;
                }
                sb.Append(ch);
            }

            // Chuẩn hóa Unicode FormD phân tách các ký tự để
            // ký tự có dấu trở thành ký tự gốc + dấu kết hợp (combining mark).
            var normalized = sb.ToString().Normalize(NormalizationForm.FormD);

            var result = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                // Bỏ qua các dấu kết hợp (thuộc danh mục NonSpacingMark)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(ch);
                }
            }

            return result.ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Trim();
        }

        public bool FuzzyContains(string? source, string? search)
        {
            var normalizedSource = Normalize(source);
            var normalizedSearch = Normalize(search);

            if (string.IsNullOrEmpty(normalizedSearch))
                return true; // chuỗi tìm kiếm rỗng thì khớp với tất cả

            if (string.IsNullOrEmpty(normalizedSource))
                return false;

            return normalizedSource.Contains(normalizedSearch, StringComparison.Ordinal);
        }

        public double Score(string? candidate, string? query)
        {
            var normalizedCandidate = Normalize(candidate);
            var normalizedQuery = Normalize(query);

            if (string.IsNullOrEmpty(normalizedQuery) || string.IsNullOrEmpty(normalizedCandidate))
                return 0.0;

            // Khớp tuyệt đối → điểm cao nhất
            if (normalizedCandidate == normalizedQuery)
                return 1.0;

            // Bắt đầu bằng → điểm cao
            if (normalizedCandidate.StartsWith(normalizedQuery, StringComparison.Ordinal))
                return 0.9;

            // Chứa từ khóa → điểm trung bình, tính trọng số theo vị trí
            var index = normalizedCandidate.IndexOf(normalizedQuery, StringComparison.Ordinal);
            if (index >= 0)
            {
                // Vị trí xuất hiện càng sớm = điểm càng cao
                var positionFactor = 1.0 - ((double)index / normalizedCandidate.Length);
                return 0.5 + (positionFactor * 0.3);
            }

            // So khớp cấp độ từ: kiểm tra xem tất cả các từ trong truy vấn có xuất hiện trong chuỗi gốc không
            var queryWords = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var candidateWords = normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (queryWords.Length > 1)
            {
                // Lọc bỏ các từ dừng (stopwords) phổ biến của tiệm bánh khi câu truy vấn có các từ khóa cụ thể
                var significantQueryWords = queryWords.Where(w => !BakeryStopWords.Contains(w)).ToArray();
                if (significantQueryWords.Length == 0)
                    significantQueryWords = queryWords;

                var matchedWords = significantQueryWords.Count(qw =>
                    candidateWords.Any(cw => cw == qw || (qw.Length >= 4 && cw.StartsWith(qw, StringComparison.Ordinal))));

                if (matchedWords > 0)
                {
                    return 0.65 * ((double)matchedWords / significantQueryWords.Length);
                }
            }

            // Thuật toán khoảng cách chỉnh sửa dự phòng để chấp nhận lỗi đánh máy (chỉ áp dụng cho truy vấn ngắn và không phải từ dừng)
            if (normalizedQuery.Length <= 20 && !BakeryStopWords.Contains(normalizedQuery))
            {
                var minDistance = int.MaxValue;
                foreach (var word in candidateWords)
                {
                    if (BakeryStopWords.Contains(word)) continue;
                    var distance = LevenshteinDistance(word, normalizedQuery);
                    if (distance < minDistance)
                        minDistance = distance;
                }

                // Cho phép sai lệch tối đa 2 ký tự cho tìm kiếm mờ (fuzzy matching)
                if (minDistance <= 2)
                {
                    return 0.2 * (1.0 - ((double)minDistance / Math.Max(normalizedQuery.Length, 1)));
                }
            }

            return 0.0;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            var m = s.Length;
            var n = t.Length;
            var d = new int[m + 1, n + 1];

            for (var i = 0; i <= m; i++) d[i, 0] = i;
            for (var j = 0; j <= n; j++) d[0, j] = j;

            for (var i = 1; i <= m; i++)
            {
                for (var j = 1; j <= n; j++)
                {
                    var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[m, n];
        }
    }
}
