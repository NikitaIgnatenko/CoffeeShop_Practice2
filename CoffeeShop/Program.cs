using CoffeeShop.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using PdfSharp;
using PdfSharp.Fonts;
using Supabase.Extensions.DependencyInjection;

namespace CoffeeShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/Login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.Name = "CoffeeShop.Auth";
                });

            builder.Services.AddSupabase(
                supabaseUrl: builder.Configuration["Supabase:Url"]!,
                supabaseKey: builder.Configuration["Supabase:Key"]!,
                configureOptions: options =>
                {
                    options.AutoRefreshToken = true;
                    options.AutoConnectRealtime = false;
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllersWithViews();

            PdfSharpLicense.SetAgplLicense();
            var fontsPath = Path.Combine(builder.Environment.WebRootPath, "fonts");
            GlobalFontSettings.FontResolver = new FontResolverHelper(fontsPath);

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapDefaultControllerRoute();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Catalog}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
