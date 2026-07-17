using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public class OrderStatusAutomationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderStatusAutomationService> _logger;

        public OrderStatusAutomationService(IServiceScopeFactory scopeFactory, ILogger<OrderStatusAutomationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("🚀 OrderStatusAutomationService đã khởi động. Kiểm tra tự động chuyển trạng thái đơn hàng mỗi 30 giây...");
            }
            catch (ObjectDisposedException) { }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAutomaticTransitionsAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    try
                    {
                        _logger.LogError(ex, "Lỗi xảy ra trong OrderStatusAutomationService.");
                    }
                    catch (ObjectDisposedException)
                    {
                        // Logger đã bị dispose do host đang tắt
                        break;
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task ProcessAutomaticTransitionsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;
            bool changed = false;

            // 1. Confirmed -> Shipped (sau 1 phút kể từ khi Confirmed)
            var confirmedOrders = await db.Orders
                .Where(o => o.Status == OrderStatuses.Confirmed && o.ConfirmedAt != null && o.ConfirmedAt.Value <= now.AddMinutes(-1))
                .ToListAsync(stoppingToken);

            foreach (var order in confirmedOrders)
            {
                order.Status = OrderStatuses.Shipped;
                order.ShippedAt = now;
                changed = true;
                _logger.LogInformation($"[Order Automation] Đơn hàng #{order.OrderId} tự động chuyển trạng thái: Confirmed -> Shipped");
            }

            // 2. Shipped -> Delivered (sau 1 phút kể từ khi Shipped)
            var shippedOrders = await db.Orders
                .Where(o => o.Status == OrderStatuses.Shipped && o.ShippedAt != null && o.ShippedAt.Value <= now.AddMinutes(-1))
                .ToListAsync(stoppingToken);

            foreach (var order in shippedOrders)
            {
                order.Status = OrderStatuses.Delivered;
                order.DeliveredAt = now;
                changed = true;
                _logger.LogInformation($"[Order Automation] Đơn hàng #{order.OrderId} tự động chuyển trạng thái: Shipped -> Delivered");
            }

            // 3. Delivered -> Completed (sau 1 phút kể từ khi Delivered)
            var deliveredOrders = await db.Orders
                .Where(o => o.Status == OrderStatuses.Delivered && o.DeliveredAt != null && o.DeliveredAt.Value <= now.AddMinutes(-1))
                .ToListAsync(stoppingToken);

            foreach (var order in deliveredOrders)
            {
                order.Status = OrderStatuses.Completed;
                order.CompletedAt = now;
                changed = true;
                _logger.LogInformation($"[Order Automation] Đơn hàng #{order.OrderId} tự động chuyển trạng thái: Delivered -> Completed");
            }

            if (changed)
            {
                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
