using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services.AI.Rag;

namespace SweetCakeShop.Services.AI
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHybridRagSearchService _hybridSearch;

        public RecommendationService(ApplicationDbContext context, IHybridRagSearchService hybridSearch)
        {
            _context = context;
            _hybridSearch = hybridSearch;
        }

        public Task<IReadOnlyList<ProductFactDto>> RecommendAsync(string userMessage, CancellationToken ct = default) =>
            RecommendWithPreferencesAsync(null, null, null, 8, ct);

        public async Task<IReadOnlyList<ProductFactDto>> RecommendWithPreferencesAsync(
            string? occasion,
            string? flavor,
            decimal? maxPrice,
            int limit = 8,
            CancellationToken ct = default)
        {
            return await _hybridSearch.RecommendProductsAsync(occasion, flavor, maxPrice, limit, ct);
        }
    }
}
