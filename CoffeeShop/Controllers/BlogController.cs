using CoffeeShop.Models;
using CoffeeShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Supabase.Postgrest;

namespace CoffeeShop.Controllers
{
    public class BlogController : Controller
    {
        private readonly Supabase.Client _supabase;
        public BlogController(Supabase.Client supabase) => _supabase = supabase;

        // GET: BlogController
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await _supabase
                .From<NewsModel>()
                .Order(n => n.CreatedAt, Constants.Ordering.Descending)
                .Get();

            var allNews = response.Models;

            var vm = new BlogViewModel
            {
                FeaturedNews = allNews.FirstOrDefault(),
                OtherNews = allNews.Skip(1).ToList()
            };

            return View(vm);
        }
    }
}