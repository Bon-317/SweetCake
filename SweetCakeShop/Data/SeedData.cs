using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Models;

namespace SweetCakeShop.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
            // Use migrations to update database schema. EnsureCreated() bypasses migrations
            // and will break migration-based workflows. Use Migrate() so pending
            // migrations are applied automatically at startup.
            context.Database.Migrate();

            Console.WriteLine("Bắt đầu seed dữ liệu mới...");

            var categories = new Category[]
            {   
                new Category { CategoryName = "Bánh kem" },
                new Category { CategoryName = "Bánh bông lan" },
                new Category { CategoryName = "Bánh quy" },
                new Category { CategoryName = "Bánh mì" },
                new Category { CategoryName = "Bánh mousse" },
                new Category { CategoryName = "Tiramisu" }
            };

            if (!context.Categories.Any())
            {
                context.Categories.AddRange(categories);
                context.SaveChanges();
            }
            else
            {
                categories = context.Categories.ToArray();
            }

            var products = new Product[]
{
                // Bánh kem
                new Product
                {
                    ProductName = "Bánh kem dâu tây",
                    Price = 350000,
                    Description = "Bánh kem tươi với dâu tây tươi ngon",
                    Image = "/images/sp2.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem socola",
                    Price = 380000,
                    Description = "Bánh kem socola Bỉ cao cấp",
                    Image = "/images/sp1.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem vanilla",
                    Price = 320000,
                    Description = "Bánh kem vanilla cổ điển",
                    Image = "/images/sp3.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem việt quất",
                    Price = 400000,
                    Description = "Bánh kem việt quất đỉnh và ngon điên",
                    Image = "/images/sp15.jpg",
                    CategoryId = categories[0].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh kem thần tiên",
                    Price = 300000,
                    Description = "Bánh kem ăn vô như lạc vào chốn bồng lai",
                    Image = "/images/sp16.jpg",
                    CategoryId = categories[0].CategoryId
                },

                // Bánh bông lan
                new Product
                {
                    ProductName = "Bánh bông lan trứng muối",
                    Price = 120000,
                    Description = "Bánh bông lan trứng muối thơm ngon",
                    Image = "/images/sp4.jpg",
                    CategoryId = categories[1].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh bông lan phô mai",
                    Price = 150000,
                    Description = "Bánh bông lan phô mai Nhật Bản",
                    Image = "/images/sp5.jpg",
                    CategoryId = categories[1].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh bông lan truyền thống",
                    Price = 90000,
                    Description = "Bánh bông lan theo công thức truyền thống",
                    Image = "/images/sp6.jpg",
                    CategoryId = categories[1].CategoryId
                },

                // Bánh quy
                new Product
                {
                    ProductName = "Bánh quy bơ",
                    Price = 80000,
                    Description = "Bánh quy bơ thơm ngon giòn tan",
                    Image = "/images/sp7.jpg",
                    CategoryId = categories[2].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh quy socola chip",
                    Price = 95000,
                    Description = "Bánh quy với socola chip cao cấp",
                    Image = "/images/sp8.jpg",
                    CategoryId = categories[2].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh quy socola bảy màu",
                    Price = 100000,
                    Description = "Bánh quy với socola với nhiều màu sắc cao cấp",
                    Image = "/images/sp18.jpg",
                    CategoryId = categories[2].CategoryId
                },new Product
                {
                    ProductName = "Bánh mì baguette",
                    Price = 25000,
                    Description = "Bánh mì baguette truyền thống Pháp",
                    Image = "/images/sp9.jpg",
                    CategoryId = categories[3].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh mì sandwich",
                    Price = 30000,
                    Description = "Bánh mì sandwich mềm mịn",
                    Image = "/images/sp10.jpg",
                    CategoryId = categories[3].CategoryId
                },
                new Product
                {
                    ProductName = "Bánh mì bơ tỏi",
                    Price = 35000,
                    Description = "Bánh mì bơ tỏi thơm phức giòn rụm",
                    Image = "/images/sp20.jpg",
                    CategoryId = categories[3].CategoryId
                },new Product
                {
                    ProductName = "Mousse socola",
                    Price = 180000,
                    Description = "Mousse socola Bỉ thơm ngon",
                    Image = "/images/sp11.jpg",
                    CategoryId = categories[4].CategoryId
                },
                new Product
                {
                    ProductName = "Mousse dâu tây",
                    Price = 170000,
                    Description = "Mousse dâu tây tươi mát",
                    Image = "/images/sp12.jpg",
                    CategoryId = categories[4].CategoryId
                },
                new Product
                {
                    ProductName = "Mousse xoài",
                    Price = 200000,
                    Description = "Mousse xoài tươi mát chua chua ngọt ngọt phù hợp cho mùa hè",
                    Image = "/images/sp19.jpg",
                    CategoryId = categories[4].CategoryId
                },new Product
                {
                    ProductName = "Tiramisu truyền thống",
                    Price = 250000,
                    Description = "Tiramisu Ý truyền thống",
                    Image = "/images/sp13.jpg",
                    CategoryId = categories[5].CategoryId
                },
                new Product
                {
                    ProductName = "Tiramisu Mango",
                    Price = 260000,
                    Description = "Tiramisu xoài tạo nên mùi vị chua ngọt thoải mái",
                    Image = "/images/sp17.jpg",
                    CategoryId = categories[5].CategoryId
                },
                new Product
                {
                    ProductName = "Tiramisu matcha",
                    Price = 280000,
                    Description = "Tiramisu với matcha Nhật Bản",
                    Image = "/images/sp14.jpg",
                    CategoryId = categories[5].CategoryId
                }
            };

            if (!context.Products.Any())
            {
                context.Products.AddRange(products);
                context.SaveChanges();
            }
            else
            {
                products = context.Products.ToArray();
            }

            var ingredients = new Ingredient[]
            {
                new Ingredient { Name = "Bột mì", Quantity = 50000, Measurement = "gram" },
                new Ingredient { Name = "Trứng gà", Quantity = 1000, Measurement = "quả" },
                new Ingredient { Name = "Đường", Quantity = 30000, Measurement = "gram" },
                new Ingredient { Name = "Sữa tươi", Quantity = 20000, Measurement = "ml" },
                new Ingredient { Name = "Bơ lạt", Quantity = 15000, Measurement = "gram" },
                new Ingredient { Name = "Kem tươi", Quantity = 12000, Measurement = "ml" },
                new Ingredient { Name = "Socola", Quantity = 10000, Measurement = "gram" },
                new Ingredient { Name = "Dâu tây", Quantity = 8000, Measurement = "gram" },
                new Ingredient { Name = "Phô mai", Quantity = 7000, Measurement = "gram" },
                new Ingredient { Name = "Bột matcha", Quantity = 2000, Measurement = "gram" }
            };

            if (!context.Ingredients.Any())
            {
                context.Ingredients.AddRange(ingredients);
                context.SaveChanges();
            }
            else
            {
                ingredients = context.Ingredients.ToArray();
            }

            if (!context.Recipes.Any())
            {
                int ProductId(string name) => products.First(p => p.ProductName == name).ProductId;
                int IngredientId(string name) => ingredients.First(i => i.Name == name).IngredientID;

                var recipes = new Recipe[]
                {
                    // Bánh kem dâu tây
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Bột mì"), Quantity = 300 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Trứng gà"), Quantity = 6 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Đường"), Quantity = 120 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Kem tươi"), Quantity = 250 },
                    new Recipe { ProductID = ProductId("Bánh kem dâu tây"), IngredientsID = IngredientId("Dâu tây"), Quantity = 200 },

                    // Bánh kem socola
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Bột mì"), Quantity = 320 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Trứng gà"), Quantity = 6 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Đường"), Quantity = 130 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Socola"), Quantity = 180 },
                    new Recipe { ProductID = ProductId("Bánh kem socola"), IngredientsID = IngredientId("Kem tươi"), Quantity = 220 },

                    // Tiramisu matcha
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Phô mai"), Quantity = 250 },
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Kem tươi"), Quantity = 180 },
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Đường"), Quantity = 90 },
                    new Recipe { ProductID = ProductId("Tiramisu matcha"), IngredientsID = IngredientId("Bột matcha"), Quantity = 25 }
                };

                context.Recipes.AddRange(recipes);
                context.SaveChanges();
            }

            var ordersNeedingConfirmedAt = context.Orders
                .Where(o => o.ConfirmedAt == null && OrderStatuses.RevenueEligibleStatuses.Contains(o.Status))
                .ToList();

            if (ordersNeedingConfirmedAt.Count > 0)
            {
                foreach (var order in ordersNeedingConfirmedAt)
                {
                    order.ConfirmedAt = order.OrderDate;
                }

                context.SaveChanges();
            }

            // ── Marketing Module Seed ──────────────────────────────────────
            if (!context.News.Any())
            {
                var news = new[]
                {
                    new News
                    {
                        Title       = "SweetCake ra mắt dòng bánh mùa hè 2026",
                        Summary     = "Những chiếc bánh mousse trái cây mát lạnh, hoàn hảo cho mùa hè nóng bức.",
                        Content     = "Mùa hè 2026, SweetCake tự hào giới thiệu bộ sưu tập bánh mới với các hương vị trái cây tươi mát như xoài, dâu tây, chanh leo. Tất cả đều được làm từ nguyên liệu tươi 100%, không chất bảo quản.",
                        ImageUrl    = "/images/sp19.jpg",
                        Author      = "Đội ngũ SweetCake",
                        PublishedAt = DateTime.Now.AddDays(-5),
                        IsPublished = true
                    },
                    new News
                    {
                        Title       = "Bí quyết bảo quản bánh kem đúng cách",
                        Summary     = "Hướng dẫn bảo quản bánh kem giữ được hương vị tươi ngon lâu nhất.",
                        Content     = "Bánh kem cần được bảo quản trong ngăn mát tủ lạnh ở nhiệt độ 2–5°C. Trước khi thưởng thức, hãy để bánh ra ngoài khoảng 15–20 phút để kem đạt độ mềm mịn tối ưu.",
                        ImageUrl    = "/images/sp1.jpg",
                        Author      = "Chef Minh Tuấn",
                        PublishedAt = DateTime.Now.AddDays(-12),
                        IsPublished = true
                    },
                    new News
                    {
                        Title       = "SweetCake đạt Top 10 thương hiệu bánh ngọt Việt Nam 2025",
                        Summary     = "Niềm vinh dự lớn khi SweetCake được bình chọn vào Top 10 thương hiệu uy tín.",
                        Content     = "Chúng tôi xin chân thành cảm ơn hàng chục nghìn khách hàng đã tin tưởng và ủng hộ SweetCake trong suốt những năm qua. Giải thưởng này là động lực để chúng tôi không ngừng cải tiến chất lượng.",
                        ImageUrl    = "/images/sp13.jpg",
                        Author      = "Ban Giám đốc SweetCake",
                        PublishedAt = DateTime.Now.AddDays(-30),
                        IsPublished = true
                    }
                };
                context.News.AddRange(news);
                context.SaveChanges();
            }

            if (!context.Promotions.Any())
            {
                var promos = new[]
                {
                    new Promotion
                    {
                        Title           = "Flash Sale Cuối Tuần – Giảm 20% Bánh Kem",
                        Description     = "Áp dụng cho tất cả sản phẩm danh mục Bánh kem. Đơn tối thiểu 300.000đ.",
                        DiscountPercent = 20,
                        BadgeText       = "HOT",
                        StartDate       = DateTime.Now.AddDays(-1),
                        EndDate         = DateTime.Now.AddDays(6),
                        ImageUrl        = "/images/sp2.jpg",
                        IsActive        = true
                    },
                    new Promotion
                    {
                        Title           = "Mua 2 Tặng 1 – Bánh Quy & Bông Lan",
                        Description     = "Mua 2 hộp bánh quy hoặc bông lan bất kỳ, tặng 1 hộp cùng loại. Áp dụng tại cửa hàng và online.",
                        DiscountPercent = null,
                        BadgeText       = "MUA 2 TẶNG 1",
                        StartDate       = DateTime.Now,
                        EndDate         = DateTime.Now.AddDays(14),
                        ImageUrl        = "/images/sp7.jpg",
                        IsActive        = true
                    },
                    new Promotion
                    {
                        Title           = "Ưu đãi thành viên mới – Giảm 15% đơn đầu tiên",
                        Description     = "Đăng ký tài khoản mới và nhận ngay voucher giảm 15% cho đơn hàng đầu tiên, không giới hạn danh mục.",
                        DiscountPercent = 15,
                        BadgeText       = "THÀNH VIÊN MỚI",
                        StartDate       = DateTime.Now.AddDays(-30),
                        EndDate         = DateTime.Now.AddDays(60),
                        ImageUrl        = "/images/sp15.jpg",
                        IsActive        = true
                    }
                };
                context.Promotions.AddRange(promos);
                context.SaveChanges();
            }

            Console.WriteLine("Seed dữ liệu hoàn tất!");
        }
    }
}
