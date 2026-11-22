using CoffeeShop.Models;

namespace CoffeeShop.Models.ViewModels
{
    public class BlogViewModel
    {
        // Самая свежая новость — выводится отдельно
        public NewsModel? FeaturedNews { get; set; }
        // Все остальные новости (в порядке от новых к старым)
        public List<NewsModel> OtherNews { get; set; } = new();
    }
}