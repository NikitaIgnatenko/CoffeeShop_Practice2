using System.Security.Claims;
using CoffeeShop.Models;
using CoffeeShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;

namespace CoffeeShop.Controllers
{
    public class CartController : Controller
    {
        private readonly Supabase.Client _supabase;
        public CartController(Supabase.Client supabase) => _supabase = supabase;

        // GET: CartController
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // проверка авторизации
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Auth");

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");

            var vm = new CartViewModel();

            // коризна и товары в ней
            var cart = (await _supabase
                .From<CartModel>()
                .Where(c => c.UsersId == userId)
                .Where(c => c.IsBlocked == false)
                .Get()).Models.FirstOrDefault();

            if (cart is null)
                return View(vm);

            var cartItems = (await _supabase
                .From<CartItemModel>()
                .Where(ci => ci.CartId == cart.Id)
                .Where(ci => ci.Quantity > 0)
                .Get()).Models;

            if (cartItems.Count == 0)
                return View(vm);

            var productIds = cartItems
                .Select(ci => ci.ProductId)
                .Distinct()
                .ToList();

            var products = (await _supabase
                .From<ProductModel>()
                .Filter("id", Constants.Operator.In, productIds)
                .Get()).Models.ToDictionary(p => p.Id);

            vm.Items = cartItems
                .Where(ci => products.ContainsKey(ci.ProductId))
                .Select(ci => new CartItemViewModel
                {
                    ProductId = ci.ProductId,
                    Name = products[ci.ProductId].Name,
                    ImageUrl = products[ci.ProductId].ImageUrl,
                    UnitPrice = products[ci.ProductId].Price,
                    Quantity = ci.Quantity
                })
                .OrderBy(i => i.Name)
                .ToList();

            vm.Message = TempData["CartMessage"] as string;

            return View(vm);
        }

        // POST: CartController/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Auth");

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");

            var cart = (await _supabase
                .From<CartModel>()
                .Where(c => c.UsersId == userId)
                .Where(c => c.IsBlocked == false)
                .Get()).Models.FirstOrDefault();

            if (cart is not null)
            {
                await _supabase
                    .From<CartItemModel>()
                    .Where(ci => ci.CartId == cart.Id)
                    .Delete();

                TempData["CartMessage"] = "Корзина очищена";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: CartController/UpdateQuantity (по сути идентичен тому, что в CatalogController)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(long productId, int delta)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Register", "Auth");

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Register", "Auth");

            if (delta == 0)
                return RedirectToAction(nameof(Index));

            var cart = (await _supabase
                .From<CartModel>()
                .Where(c => c.UsersId == userId)
                .Where(c => c.IsBlocked == false)
                .Get()).Models.FirstOrDefault();

            if (cart is null)
                return RedirectToAction(nameof(Index));

            var item = (await _supabase
                .From<CartItemModel>()
                .Where(ci => ci.CartId == cart.Id)
                .Where(ci => ci.ProductId == productId)
                .Get()).Models.FirstOrDefault();

            if (item is null)
                return RedirectToAction(nameof(Index));

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

            return RedirectToAction(nameof(Index));
        }
    }
}