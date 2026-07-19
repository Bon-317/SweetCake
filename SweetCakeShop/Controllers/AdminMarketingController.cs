using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Controllers
{
    /// <summary>
    /// Admin CRUD cho module Marketing: News, Promotions, Newsletter Subscribers.
    /// </summary>
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminMarketingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminMarketingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        //  NEWS
        // =========================================================

        #region News

        [HttpGet]
        public async Task<IActionResult> News()
        {
            var news = await _context.News
                .OrderByDescending(n => n.PublishedAt)
                .ToListAsync();
            return View(news);
        }

        [HttpGet]
        public IActionResult CreateNews()
        {
            return View(new News { PublishedAt = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNews(News model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.News.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo bài tin tức thành công!";
            return RedirectToAction(nameof(News));
        }

        [HttpGet]
        public async Task<IActionResult> EditNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();
            return View(news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNews(News model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.News.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật bài tin tức thành công!";
            return RedirectToAction(nameof(News));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news != null)
            {
                _context.News.Remove(news);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa bài tin tức thành công!";
            }
            return RedirectToAction(nameof(News));
        }

        #endregion

        // =========================================================
        //  PROMOTIONS
        // =========================================================

        #region Promotions

        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            var promos = await _context.Promotions
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
            return View(promos);
        }

        [HttpGet]
        public IActionResult CreatePromotion()
        {
            return View(new Promotion
            {
                StartDate = DateTime.Now,
                EndDate   = DateTime.Now.AddDays(7),
                IsActive  = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePromotion(Promotion model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Promotions.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo khuyến mãi thành công!";
            return RedirectToAction(nameof(Promotions));
        }

        [HttpGet]
        public async Task<IActionResult> EditPromotion(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPromotion(Promotion model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Promotions.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật khuyến mãi thành công!";
            return RedirectToAction(nameof(Promotions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null)
            {
                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa khuyến mãi thành công!";
            }
            return RedirectToAction(nameof(Promotions));
        }

        #endregion

        // =========================================================
        //  NEWSLETTER SUBSCRIBERS
        // =========================================================

        #region Subscribers

        [HttpGet]
        public async Task<IActionResult> Subscribers()
        {
            var subs = await _context.NewsletterSubscribers
                .OrderByDescending(s => s.SubscribedAt)
                .ToListAsync();
            return View(subs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubscriber(int id)
        {
            var sub = await _context.NewsletterSubscribers.FindAsync(id);
            if (sub != null)
            {
                _context.NewsletterSubscribers.Remove(sub);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa subscriber thành công!";
            }
            return RedirectToAction(nameof(Subscribers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubscriber(int id)
        {
            var sub = await _context.NewsletterSubscribers.FindAsync(id);
            if (sub != null)
            {
                sub.IsActive = !sub.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = sub.IsActive ? "Đã kích hoạt lại subscriber." : "Đã vô hiệu hóa subscriber.";
            }
            return RedirectToAction(nameof(Subscribers));
        }

        #endregion
    }
}
