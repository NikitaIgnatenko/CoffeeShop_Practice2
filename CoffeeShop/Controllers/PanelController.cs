using CoffeeShop.Models;
using CoffeeShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;
using System.Security.Claims;

namespace CoffeeShop.Controllers
{
    public class PanelController : Controller
    {
        private readonly Supabase.Client _supabase;

        public PanelController(Supabase.Client supabase) => _supabase = supabase;
        private bool IsAdmin()
        {
            return User.Identity?.IsAuthenticated == true &&
                   User.HasClaim("IsAdmin", "true");
        }

        private IActionResult CheckAccess()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Catalog");
            return null!;
        }

        // GET: Admin
        [HttpGet]
        public async Task<IActionResult> Index(string tab = "content")
        {
            var check = CheckAccess();
            if (check != null) return check;

            var vm = new AdminViewModel { ActiveTab = tab };

            if (tab == "content")
            {
                var response = (await _supabase.From<NewsModel>()
                    .Order(n => n.CreatedAt, Constants.Ordering.Descending)
                    .Get());
                vm.News = response.Models ?? new List<NewsModel>();
            }
            else if (tab == "products")
            {
                var response = (await _supabase.From<ProductModel>()
                    .Order(p => p.Name, Constants.Ordering.Ascending)
                    .Get());
                vm.Products = response.Models ?? new List<ProductModel>();
            }
            else if (tab == "stats")
            {
                vm.Stats = await CalculateStatsAsync() ?? new AdminStatsViewModel();
            }

            return View(vm);
        }

        // --- УПРАВЛЕНИЕ НОВОСТЯМИ ---
        // GET: Admin/EditNews
        [HttpGet]
        public async Task<IActionResult> EditNews(long? id)
        {
            var check = CheckAccess();
            if (check != null) return check;

            if (id == null)
                return View(new AdminNewsEditViewModel()); // Добавление

            var news = (await _supabase.From<NewsModel>().Where(n => n.Id == id).Get()).Models.FirstOrDefault();
            if (news == null) return NotFound();

            var vm = new AdminNewsEditViewModel
            {
                Id = news.Id,
                Title = news.Title,
                Description = news.Description,
                ExistingImageUrl = news.ImageUrl
            };
            return View(vm);
        }

        // POST: Admin/EditNews
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNews(AdminNewsEditViewModel model)
        {
            var check = CheckAccess();
            if (check != null) return check;

            if (!ModelState.IsValid) return View(model);

            string imageUrl = model.ExistingImageUrl ?? "";

            // Если загружено новое изображение
            if (model.NewImage != null && model.NewImage.Length > 0)
            {
                imageUrl = await UploadImageAsync(model.NewImage, "news_img");
            }

            if (model.Id == 0) // Добавление
            {
                await _supabase.From<NewsModel>().Insert(new NewsModel
                {
                    Title = model.Title,
                    Description = model.Description,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else // Редактирование
            {
                var news = (await _supabase.From<NewsModel>().Where(n => n.Id == model.Id).Get()).Models.FirstOrDefault();
                if (news != null)
                {
                    news.Title = model.Title;
                    news.Description = model.Description;
                    news.ImageUrl = imageUrl;
                    await _supabase.From<NewsModel>().Update(news);
                }
            }

            return RedirectToAction(nameof(Index), new { tab = "content" });
        }

        // --- УПРАВЛЕНИЕ ТОВАРАМИ ---
        // GET: Admin/EditProduct
        [HttpGet]
        public async Task<IActionResult> EditProduct(long? id)
        {
            var check = CheckAccess();
            if (check != null) return check;

            var vm = new AdminProductEditViewModel
            {
                ProductTypes = (await _supabase.From<ProductTypeModel>().Get()).Models
            };

            if (id != null)
            {
                var product = (await _supabase.From<ProductModel>().Where(p => p.Id == id).Get()).Models.FirstOrDefault();
                if (product == null) return NotFound();

                vm.Id = product.Id;
                vm.Name = product.Name;
                vm.Description = product.Description;
                vm.ExistingImageUrl = product.ImageUrl;
                vm.ProductTypeId = product.ProductTypeId;

                // Передаем цену в представление
                vm.Price = product.Price;
            }

            return View(vm);
        }

        // POST: Admin/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(AdminProductEditViewModel model)
        {
            var check = CheckAccess();
            if (check != null) return check;

            if (!ModelState.IsValid)
            {
                model.ProductTypes = (await _supabase.From<ProductTypeModel>().Get()).Models;
                return View(model);
            }

            string imageUrl = model.ExistingImageUrl ?? "";

            if (model.NewImage != null && model.NewImage.Length > 0)
            {
                imageUrl = await UploadImageAsync(model.NewImage, "catalog_img");
            }

            if (model.Id == 0) // Добавление
            {
                await _supabase.From<ProductModel>().Insert(new ProductModel
                {
                    Name = model.Name,
                    Description = model.Description,
                    ImageUrl = imageUrl,
                    ProductTypeId = model.ProductTypeId,
                    Price = model.Price // Сохраняем цену
                });
            }
            else // Редактирование
            {
                var product = (await _supabase.From<ProductModel>().Where(p => p.Id == model.Id).Get()).Models.FirstOrDefault();
                if (product != null)
                {
                    product.Name = model.Name;
                    product.Description = model.Description;
                    product.ImageUrl = imageUrl;
                    product.ProductTypeId = model.ProductTypeId;
                    product.Price = model.Price; // Обновляем цену

                    await _supabase.From<ProductModel>().Update(product);
                }
            }

            return RedirectToAction(nameof(Index), new { tab = "products" });
        }

        // POST: Admin/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var check = CheckAccess();
            if (check != null) return check;

            await _supabase.From<ProductModel>().Where(p => p.Id == id).Delete();
            return RedirectToAction(nameof(Index), new { tab = "products" });
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        private async Task<string> UploadImageAsync(IFormFile file, string bucketName)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            var storage = _supabase.Storage.From(bucketName);
            await storage.Upload(bytes, fileName, new Supabase.Storage.FileOptions { ContentType = file.ContentType });

            return storage.GetPublicUrl(fileName);
        }

        private async Task<AdminStatsViewModel> CalculateStatsAsync()
        {
            var orders = (await _supabase.From<OrderModel>().Get()).Models;
            var cartItems = (await _supabase.From<CartItemModel>().Get()).Models;
            var products = (await _supabase.From<ProductModel>().Get()).Models;

            var stats = new AdminStatsViewModel();

            if (orders.Any())
            {
                decimal totalRevenue = orders.Sum(o => o.FinalPrice);
                stats.AverageCheck = totalRevenue / orders.Count;

                // Маржа = (30% от каждого заказа * Пи)
                decimal marginCost = totalRevenue * 0.3m * (decimal)Math.PI;
                stats.NetProfit = totalRevenue - marginCost;
                stats.MarginPercent = totalRevenue > 0 ? (stats.NetProfit / totalRevenue) * 100 : 0;

                // Средняя ценность клиента (медиана)
                var userTotals = orders.GroupBy(o => o.UserId)
                    .Select(g => g.Sum(o => o.FinalPrice))
                    .OrderBy(x => x)
                    .ToList();

                if (userTotals.Any())
                {
                    int mid = userTotals.Count / 2;
                    stats.AverageCustomerValue = userTotals.Count % 2 != 0
                        ? userTotals[mid]
                        : (userTotals[mid - 1] + userTotals[mid]) / 2;
                }

                // Процент возврата
                var totalUsers = orders.Select(o => o.UserId).Distinct().Count();
                var returningUsers = orders.GroupBy(o => o.UserId).Count(g => g.Count() > 1);
                stats.ReturnPercentage = totalUsers > 0 ? ((double)returningUsers / totalUsers) * 100 : 0;
            }

            // Популярные/непопулярные товары
            if (cartItems.Any() && products.Any())
            {
                var productGroups = cartItems.GroupBy(ci => ci.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(ci => ci.Quantity) })
                    .OrderByDescending(x => x.TotalQty)
                    .ToList();

                var mostPopular = productGroups.FirstOrDefault();
                if (mostPopular != null)
                    stats.MostPopularProduct = products.FirstOrDefault(p => p.Id == mostPopular.ProductId)?.Name ?? "Нет данных";

                // Ищем наименее популярный только среди тех, что вообще есть в корзинах
                var leastPopular = productGroups.LastOrDefault();
                if (leastPopular != null)
                    stats.LeastPopularProduct = products.FirstOrDefault(p => p.Id == leastPopular.ProductId)?.Name ?? "Нет данных";
            }

            return stats;
        }
    }
}