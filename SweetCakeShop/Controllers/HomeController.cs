using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using SweetCakeShop.Services;
using System.Diagnostics;

namespace SweetCakeShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMarketingService _marketingService;

        public HomeController(ApplicationDbContext context, IMarketingService marketingService)
        {
            _context = context;
            _marketingService = marketingService;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _context.Products
                .OrderBy(p => p.ProductId)
                .Take(5)
                .ToListAsync();

            var model = new HomeViewModel
            {
                FeaturedProducts = featuredProducts
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> About()
        {
            var vm = new AboutViewModel
            {
                FeaturedProducts = await _marketingService.GetFeaturedProductsAsync(8),
                LatestNews       = await _marketingService.GetLatestNewsAsync(3),
                ActivePromotions = await _marketingService.GetActivePromotionsAsync(),
                NewsletterForm   = new NewsletterSubscribeViewModel()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> About(AboutViewModel model)
        {
            // Chỉ validate phần NewsletterForm
            if (!ModelState.IsValid)
            {
                // Nạp lại dữ liệu các section khác
                model.FeaturedProducts = await _marketingService.GetFeaturedProductsAsync(8);
                model.LatestNews       = await _marketingService.GetLatestNewsAsync(3);
                model.ActivePromotions = await _marketingService.GetActivePromotionsAsync();
                return View(model);
            }

            var success = await _marketingService.SubscribeNewsletterAsync(model.NewsletterForm.Email);
            if (success)
                TempData["NewsletterSuccess"] = "Đăng ký thành công! Cảm ơn bạn đã đăng ký nhận tin từ SweetCake 🍰";
            else
                TempData["NewsletterDuplicate"] = "Email này đã đăng ký trước đó rồi. Cảm ơn bạn!";

            return RedirectToAction(nameof(About));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult IndexContact()
        {
            return View(new ContactFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IndexContact(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["ContactSuccess"] = "Gửi tin nhắn thành công! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
            return RedirectToAction(nameof(IndexContact));
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View("IndexContact", new ContactFormViewModel());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
