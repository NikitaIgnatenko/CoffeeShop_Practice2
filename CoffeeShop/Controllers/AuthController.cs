using CoffeeShop.Helpers;
using CoffeeShop.Models;
using CoffeeShop.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.Security.Claims;

namespace CoffeeShop.Controllers
{
    public class AuthController : Controller
    {
        private readonly Client _supabase;

        public AuthController(Client supabase)
        {
            _supabase = supabase;
        }

        // ---------------- ЛОГИН ----------------

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Catalog");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Ищем пользователя по номеру телефона (номер = логин)
            var response = await _supabase
                .From<UserModel>()
                .Where(u => u.Phone == model.Phone)
                .Get();

            var user = response.Models.FirstOrDefault();

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Пользователь с таким номером телефона не найден");
                return View(model);
            }

            if (!PasswordHelper.Verify(model.Password, user.PassHash))
            {
                ModelState.AddModelError(string.Empty, "Неверный пароль");
                return View(model);
            }

            // Формируем Claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name,           user.Name),
                new(ClaimTypes.MobilePhone,    user.Phone),
                new("IsAdmin",                 user.IsAdmin.ToString().ToLowerInvariant())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            return RedirectToAction("Index", "Catalog");
        }

        // ---------------- РЕГИСТРАЦИЯ ----------------

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Catalog");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Проверка: существует ли уже такой номер телефона
            var existing = await _supabase
                .From<UserModel>()
                .Where(u => u.Phone == model.Phone)
                .Get();

            if (existing.Models.Any())
            {
                ModelState.AddModelError(nameof(model.Phone),
                    "Пользователь с таким номером телефона уже зарегистрирован");
                return View(model);
            }

            var newUser = new UserModel
            {
                Name = model.Name,
                Phone = model.Phone,
                PassHash = PasswordHelper.HashSha512(model.Password),
                IsAdmin = false          // явно указываем false по умолчанию
            };

            await _supabase.From<UserModel>().Insert(newUser);

            // Автоматический редирект на страницу авторизации
            return RedirectToAction(nameof(Login));
        }

        // ---------------- ВЫХОД ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}