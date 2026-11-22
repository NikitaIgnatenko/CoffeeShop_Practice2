using CoffeeShop.Models;
using CoffeeShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Supabase.Postgrest;
using System.Security.Claims;

namespace CoffeeShop.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly Supabase.Client _supabase;
        public CheckoutController(Supabase.Client supabase) => _supabase = supabase;

        // GET: CheckoutController
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // проверка авторизации
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Auth");

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");

            // данные пользователя
            var user = (await _supabase
                .From<UserModel>()
                .Where(u => u.Id == userId)
                .Get()).Models.FirstOrDefault();

            if (user is null)
                return RedirectToAction("Login", "Auth");

            var vm = new CheckoutViewModel
            {
                UserName = user.Name,
                UserPhone = user.Phone
            };

            vm.Branches = (await _supabase
                .From<BranchModel>()
                .Order(b => b.Address, Constants.Ordering.Ascending)
                .Get()).Models;

            // корзина пользователя
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

            vm.Cart.Items = cartItems
                .Where(ci => products.ContainsKey(ci.ProductId))
                .Select(ci => new CartItemViewModel
                {
                    ProductId = ci.ProductId,
                    Name = products[ci.ProductId].Name,
                    UnitPrice = products[ci.ProductId].Price,
                    Quantity = ci.Quantity,
                })
                .OrderBy(i => i.Name)
                .ToList();

            return View(vm);
        }

        // POST: Checkout/Complete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(long? branchId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Auth");

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");

            if (branchId == null)
            {
                TempData["CheckoutError"] = "Выберите пункт выдачи";
                return RedirectToAction(nameof(Index));
            }

            // Получаем корзину пользователя
            var cart = (await _supabase
                .From<CartModel>()
                .Where(c => c.UsersId == userId)
                .Where(c => c.IsBlocked == false)
                .Get()).Models.FirstOrDefault();

            if (cart == null)
            {
                TempData["CheckoutError"] = "Корзина не найдена";
                return RedirectToAction(nameof(Index));
            }

            // Получаем позиции корзины
            var cartItems = (await _supabase
                .From<CartItemModel>()
                .Where(ci => ci.CartId == cart.Id)
                .Where(ci => ci.Quantity > 0)
                .Get()).Models;

            if (cartItems.Count == 0)
            {
                TempData["CheckoutError"] = "Корзина пуста";
                return RedirectToAction(nameof(Index));
            }

            // Подгружаем товары для расчёта итоговой суммы
            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var products = (await _supabase
                .From<ProductModel>()
                .Filter("id", Constants.Operator.In, productIds)
                .Get()).Models.ToDictionary(p => p.Id);

            decimal total = 0;
            foreach (var ci in cartItems)
            {
                if (products.TryGetValue(ci.ProductId, out var product))
                    total += product.Price * ci.Quantity;
            }

            // Создаём заказ
            var order = new OrderModel
            {
                UserId = userId,
                CartId = cart.Id,
                BranchId = branchId.Value,
                FinalPrice = total
            };

            var insertedOrder = await _supabase.From<OrderModel>().Insert(order);
            var createdOrder = insertedOrder.Models.First();

            // Блокируем корзину
            cart.IsBlocked = true;
            await _supabase.From<CartModel>().Update(cart);

            // Сохраняем ID заказа для страницы подтверждения
            TempData["OrderId"] = createdOrder.Id.ToString();

            return RedirectToAction(nameof(Confirmation));
        }

        // GET: Checkout/Confirmation
        [HttpGet]
        public IActionResult Confirmation()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Auth");

            var orderId = TempData["OrderId"];
            if (orderId == null)
                return RedirectToAction(nameof(Index));

            ViewBag.OrderId = orderId;
            return View();
        }

        // POST: Checkout/PrintReceipt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintReceipt(long orderId)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Auth");

            // Загружаем заказ
            var order = (await _supabase
                .From<OrderModel>()
                .Where(o => o.Id == orderId)
                .Get()).Models.FirstOrDefault();

            if (order == null) return NotFound();

            // Загружаем связанные данные
            var user = (await _supabase
                .From<UserModel>()
                .Where(u => u.Id == order.UserId)
                .Get()).Models.FirstOrDefault();

            var branch = (await _supabase
                .From<BranchModel>()
                .Where(b => b.Id == order.BranchId)
                .Get()).Models.FirstOrDefault();

            var cartItems = (await _supabase
                .From<CartItemModel>()
                .Where(ci => ci.CartId == order.CartId)
                .Get()).Models;

            var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var products = (await _supabase
                .From<ProductModel>()
                .Filter("id", Constants.Operator.In, productIds)
                .Get()).Models.ToDictionary(p => p.Id);

            // Генерация PDF
            using var ms = new MemoryStream();
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);

            double y = 40;
            gfx.DrawString("Чек заказа", titleFont, XBrushes.Black, new XPoint(40, y));
            y += 30;
            gfx.DrawString($"Номер заказа: {order.Id}", font, XBrushes.Black, new XPoint(40, y));
            y += 20;
            gfx.DrawString($"Имя: {user?.Name ?? "Неизвестно"}", font, XBrushes.Black, new XPoint(40, y));
            y += 20;
            gfx.DrawString($"Телефон: {user?.Phone ?? "Неизвестно"}", font, XBrushes.Black, new XPoint(40, y));
            y += 20;
            gfx.DrawString($"Пункт выдачи: {branch?.Address ?? "Неизвестно"}", font, XBrushes.Black, new XPoint(40, y));
            y += 30;
            gfx.DrawString("Товары:", titleFont, XBrushes.Black, new XPoint(40, y));
            y += 25;

            foreach (var ci in cartItems)
            {
                if (products.TryGetValue(ci.ProductId, out var p))
                {
                    var subtotal = p.Price * ci.Quantity;
                    gfx.DrawString($"{p.Name} x{ci.Quantity} = {subtotal:C}", font, XBrushes.Black, new XPoint(40, y));
                    y += 20;
                }
            }

            y += 10;
            gfx.DrawString($"Итого: {order.FinalPrice:C}", titleFont, XBrushes.Black, new XPoint(40, y));

            document.Save(ms, false);
            ms.Position = 0;

            return File(ms.ToArray(), "application/pdf", $"receipt_{order.Id}.pdf");
        }
    }
}