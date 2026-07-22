using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services.AI.Rag;

namespace SweetCakeShop.Services.AI
{
    public class ProductAnalyticsService : IProductAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHybridRagSearchService _hybridSearch;

        public ProductAnalyticsService(ApplicationDbContext context, IHybridRagSearchService hybridSearch)
        {
            _context = context;
            _hybridSearch = hybridSearch;
        }

        public async Task<ProductFactDto?> GetHighestPriceAsync(CancellationToken ct = default)
        {
            var p = await (
                from prod in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on prod.CategoryId equals c.CategoryId
                orderby prod.Price descending
                select new ProductFactDto
                {
                    ProductId = prod.ProductId,
                    Name = prod.ProductName,
                    Price = prod.Price,
                    Category = c.CategoryName,
                    Description = prod.Description,
                    ImageUrl = prod.Image
                }).FirstOrDefaultAsync(ct);
            return p;
        }

        public async Task<ProductFactDto?> GetLowestPriceAsync(CancellationToken ct = default)
        {
            var p = await (
                from x in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on x.CategoryId equals c.CategoryId
                orderby x.Price
                select new ProductFactDto
                {
                    ProductId = x.ProductId,
                    Name = x.ProductName,
                    Price = x.Price,
                    Category = c.CategoryName,
                    ImageUrl = x.Image
                }).FirstOrDefaultAsync(ct);
            return p;
        }

        public async Task<IReadOnlyList<ProductFactDto>> GetTopSellingAsync(int take = 5, CancellationToken ct = default)
        {
            var items = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by new { p.ProductName, p.Price } into g
                orderby g.Sum(x => x.Quantity) descending
                select new ProductFactDto
                {
                    Name = g.Key.ProductName,
                    Price = g.Key.Price,
                    SoldQuantity = g.Sum(x => x.Quantity)
                }).Take(take).ToListAsync(ct);
            return items;
        }

        public async Task<IReadOnlyList<ProductFactDto>> GetWorstSellingAsync(int take = 3, CancellationToken ct = default)
        {
            var items = await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.ConfirmedAt != null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status)
                group od by new { p.ProductName, p.Price } into g
                orderby g.Sum(x => x.Quantity)
                select new ProductFactDto
                {
                    Name = g.Key.ProductName,
                    Price = g.Key.Price,
                    SoldQuantity = g.Sum(x => x.Quantity)
                }).Take(take).ToListAsync(ct);
            return items;
        }

        public async Task<IReadOnlyList<ProductFactDto>> GetCatalogAsync(int take = 12, CancellationToken ct = default)
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

        public async Task<ProductFactDto?> FindByNameAsync(string name, CancellationToken ct = default)
        {
            var term = name.Trim();
            if (string.IsNullOrEmpty(term)) return null;

            var p = await (
                from prod in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on prod.CategoryId equals c.CategoryId
                where prod.ProductName.Contains(term)
                orderby prod.ProductName.Length
                select new ProductFactDto
                {
                    ProductId = prod.ProductId,
                    Name = prod.ProductName,
                    Price = prod.Price,
                    Category = c.CategoryName,
                    Description = prod.Description,
                    ImageUrl = prod.Image
                }).FirstOrDefaultAsync(ct);
            return p;
        }

        public async Task<IReadOnlyList<ProductFactDto>> SearchProductsAsync(string query, int take = 8, CancellationToken ct = default)
        {
            return await _hybridSearch.SearchProductsAsync(query, take, fallbackToCatalog: true, ct);
        }

        public async Task<ProductFactDto?> GetProductDetailsAsync(string productName, CancellationToken ct = default) =>
            await FindByNameAsync(productName, ct);

        public async Task<IReadOnlyList<ProductFactDto>> GetRelatedProductsAsync(string productName, int take = 6, CancellationToken ct = default)
        {
            var anchor = await FindByNameAsync(productName, ct);
            if (anchor == null || string.IsNullOrWhiteSpace(anchor.Category))
                return await GetTopSellingAsync(take, ct);

            return await (
                from p in _context.Products.AsNoTracking()
                join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.CategoryId
                where c.CategoryName == anchor.Category && p.ProductName != anchor.Name
                orderby p.Price
                select new ProductFactDto
                {
                    ProductId = p.ProductId,
                    Name = p.ProductName,
                    Price = p.Price,
                    Category = c.CategoryName,
                    Description = p.Description
                }).Take(take).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default) =>
            await _context.Categories.AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .Select(c => c.CategoryName)
                .ToListAsync(ct);
    }
}
