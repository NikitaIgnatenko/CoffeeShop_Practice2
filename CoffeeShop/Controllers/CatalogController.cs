using CoffeeShop.Models;
using CoffeeShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;
using System.Security.Claims;

namespace CoffeeShop.Controllers
{
    public class CatalogController : Controller
    {
        private readonly Supabase.Client _supabase;
        public CatalogController(Supabase.Client supabase) => _supabase = supabase;

        // GET: CatalogController
        [HttpGet]
        public async Task<IActionResult> Index(long? productTypeId, int page = 1)
        {
            if (page < 1) page = 1;

            // формирование каталога
            var productTypes = (await _supabase
                .From<ProductTypeModel>()
                .Get()).Models;

            var productQuery = _supabase.From<ProductModel>();
            if (productTypeId.HasValue)
                productQuery = (Supabase.Interfaces.ISupabaseTable<ProductModel, Supabase.Realtime.RealtimeChannel>)
                    productQuery.Where(e => e.ProductTypeId == productTypeId.Value);

            var pageSize = 18;
            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            var productsResponse = await productQuery
                .Order(s => s.Name, Constants.Ordering.Ascending)
                .Range(from, to)
                .Get();

            var totalCount = await productQuery.Count(Constants.CountType.Exact);
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

            if (page > totalPages && totalPages > 0)
                return RedirectToAction(nameof(Index), new
                {
                    productTypeId,
                    page = totalPages
                });

            CatalogViewModel vm = new CatalogViewModel()
            {
                ProductTypes = productTypes,
                Products = productsResponse.Models,
                CartQuantities = await LoadCartQuantitiesAsync(),
                SelectedProductTypeId = productTypeId,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // POST: CatalogController/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(long productId, long? productTypeId, int page = 1)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Register", "Auth");

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Register", "Auth");

            var product = (await _supabase
                .From<ProductModel>()
                .Where(p => p.Id == productId)
                .Get()).Models.FirstOrDefault();

            if (product is null)
            {
                TempData["CartMessage"] = "Товар не найден";
                return RedirectToAction(nameof(Index), new { productTypeId, page });
            }

            var cart = await GetOrCreateCartAsync(userId);

            var existing = await _supabase
                .From<CartItemModel>()
                .Where(ci => ci.CartId == cart.Id)
                .Where(ci => ci.ProductId == productId)
                .Where(ci => ci.Quantity > 0)
                .Get();

            if (existing.Models.Any())
            {
                TempData["CartMessage"] = "Этот товар уже есть в корзине";
                return RedirectToAction(nameof(Index), new { productTypeId, page });
            }

            var productType = (await _supabase
                .From<ProductTypeModel>()
                .Where(pt => pt.Id == product.ProductTypeId)
                .Get()).Models.FirstOrDefault();

            await _supabase.From<CartItemModel>().Insert(new CartItemModel
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = 1,
                IsCoffee = IsCoffeeType(productType?.Name)
            });

            TempData["CartMessage"] = "Товар добавлен в корзину";
            return RedirectToAction(nameof(Index), new { productTypeId, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(long productId, int delta, long? productTypeId, int page = 1)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Register", "Auth");

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Register", "Auth");

            if (delta == 0)
                return RedirectToAction(nameof(Index), new { productTypeId, page });

            var cart = (await _supabase
                .From<CartModel>()
                .Where(c => c.UsersId == userId)
                .Where(c => c.IsBlocked == false)
                .Get()).Models.FirstOrDefault();

            if (cart is null)
                return RedirectToAction(nameof(Index), new { productTypeId, page });

            var item = (await _supabase
                .From<CartItemModel>()
                .Where(ci => ci.CartId == cart.Id)
                .Where(ci => ci.ProductId == productId)
                .Get()).Models.FirstOrDefault();

            if (item is null)
                return RedirectToAction(nameof(Index), new { productTypeId, page });

            var newQuantity = item.Quantity + delta;

            if (newQuantity <= 0)
            {
                await _supabase
                    .From<CartItemModel>()
                    .Where(ci => ci.Id == item.Id)
                    .Delete();
            }
            else
            {
                item.Quantity = newQuantity;
                await _supabase.From<CartItemModel>().Update(item);
            }

            return RedirectToAction(nameof(Index), new { productTypeId, page });
        }

        private static bool IsCoffeeType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return false;
            return typeName.Contains("кофе", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("coffee", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<CartModel> GetOrCreateCartAsync(long userId)
        {
            var cart = (await _supabase
                .From<CartModel>()
                .Where(c => c.UsersId == userId)
                .Where(c => c.IsBlocked == false)
                .Get()).Models.FirstOrDefault();

            if (cart is not null) return cart;

            var inserted = await _supabase
                .From<CartModel>()
                .Insert(new CartModel { UsersId = userId, IsBlocked = false });

            return inserted.Models.First();
        }

        private async Task<Dictionary<long, long>> LoadCartQuantitiesAsync()
        {
            var result = new Dictionary<long, long>();

            if (User.Identity?.IsAuthenticated != true) return result;
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return result;

            var cart = (await _supabase
                .From<CartModel>()
                .Where(c => c.UsersId == userId)
                .Where(c => c.IsBlocked == false)
                .Get()).Models.FirstOrDefault();

            if (cart is null) return result;

            var items = (await _supabase
                .From<CartItemModel>()
                .Where(ci => ci.CartId == cart.Id)
                .Where(ci => ci.Quantity > 0)
                .Get()).Models;

            foreach (var it in items)
                result[it.ProductId] = it.Quantity;

            return result;
        }
    }
}
