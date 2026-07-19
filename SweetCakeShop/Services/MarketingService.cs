using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public class MarketingService : IMarketingService
    {
        private readonly ApplicationDbContext _context;

        public MarketingService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<News>> GetLatestNewsAsync(int count = 3)
        {
            return await _context.News
                .AsNoTracking()
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishedAt)
                .Take(count)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Promotion>> GetActivePromotionsAsync()
        {
            var now = DateTime.Now;
            return await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Product>> GetFeaturedProductsAsync(int count = 8)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderByDescending(p => p.ProductId)
                .Take(count)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> SubscribeNewsletterAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalized = email.Trim().ToLowerInvariant();

            var exists = await _context.NewsletterSubscribers
                .AnyAsync(s => s.Email == normalized);

            if (exists)
                return false;

            _context.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email        = normalized,
                SubscribedAt = DateTime.Now,
                IsActive     = true
            });

            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> IsEmailSubscribedAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalized = email.Trim().ToLowerInvariant();
            return await _context.NewsletterSubscribers
                .AnyAsync(s => s.Email == normalized);
        }
    }
}
