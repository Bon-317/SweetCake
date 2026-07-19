using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public class OrderInventoryService : IOrderInventoryService
    {
        private readonly ApplicationDbContext _context;

        public OrderInventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> DeductInventoryForOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return (false, "Không tìm thấy đơn hàng");

            if (!order.OrderDetails.Any())
                return (false, "Đơn hàng không có sản phẩm nào");

            // Nếu đơn hàng đã là Confirmed/Shipped/Delivered/Completed/Cancelled thì không trừ kho lại
            if (order.Status == OrderStatuses.Confirmed || order.Status == OrderStatuses.Shipped || 
                order.Status == OrderStatuses.Delivered || order.Status == OrderStatuses.Completed || 
                order.Status == OrderStatuses.Cancelled ||
                (order.ConfirmedAt != null && order.Status != OrderStatuses.Pending && order.Status != "AwaitingPayment" && order.Status != "AwaitingConfirmation"))
            {
                return (true, $"Đơn hàng đã được xác nhận và trừ kho trước đó ({order.Status})");
            }

            var productIds = order.OrderDetails.Select(od => od.ProductId).Distinct().ToList();
            var recipes = await _context.Recipes.Where(r => productIds.Contains(r.ProductID)).ToListAsync();
            var recipesByProduct = recipes.GroupBy(r => r.ProductID).ToDictionary(g => g.Key, g => g.ToList());

            var requiredByIngredient = new Dictionary<int, decimal>();

            foreach (var detail in order.OrderDetails)
            {
                if (!recipesByProduct.TryGetValue(detail.ProductId, out var productRecipe) || productRecipe.Count == 0)
                {
                    return (false, $"Sản phẩm ID {detail.ProductId} chưa thiết lập công thức nguyên liệu");
                }

                foreach (var recipe in productRecipe)
                {
                    var required = recipe.Quantity * detail.Quantity;
                    if (requiredByIngredient.ContainsKey(recipe.IngredientsID))
                        requiredByIngredient[recipe.IngredientsID] += required;
                    else
                        requiredByIngredient[recipe.IngredientsID] = required;
                }
            }

            var ingredientIds = requiredByIngredient.Keys.ToList();
            var ingredients = await _context.Ingredients.Where(i => ingredientIds.Contains(i.IngredientID)).ToListAsync();

            if (ingredients.Count != ingredientIds.Count)
            {
                return (false, "Một số nguyên liệu trong công thức không còn tồn tại trong kho");
            }

            // Kiểm tra đủ nguyên liệu
            foreach (var ingredient in ingredients)
            {
                var required = requiredByIngredient[ingredient.IngredientID];
                if (ingredient.Quantity < required)
                {
                    return (false, $"Nguyên liệu '{ingredient.Name}' không đủ (Cần: {required}, Hiện có: {ingredient.Quantity})");
                }
            }

            // Trừ kho + Cập nhật trạng thái Confirmed
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var ingredient in ingredients)
                {
                    var required = requiredByIngredient[ingredient.IngredientID];
                    ingredient.Quantity = Math.Round(ingredient.Quantity - required, 2, MidpointRounding.AwayFromZero);
                }

                if (order.Status != OrderStatuses.Confirmed)
                {
                    order.Status = OrderStatuses.Confirmed;
                }
                if (order.ConfirmedAt == null)
                {
                    order.ConfirmedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, $"Đã trừ nguyên liệu và xác nhận đơn hàng #{order.OrderId} thành công!");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Lỗi khi trừ kho: {ex.Message}");
            }
        }
    }
}
