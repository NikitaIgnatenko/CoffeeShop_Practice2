using CoffeeShop.Models;
using System.ComponentModel.DataAnnotations;

namespace CoffeeShop.Models.ViewModels
{
    public class AdminViewModel
    {
        public string ActiveTab { get; set; } = "content";
        public List<NewsModel> News { get; set; } = new();
        public List<ProductModel> Products { get; set; } = new();
        public AdminStatsViewModel? Stats { get; set; }
    }

    public class AdminStatsViewModel
    {
        public decimal AverageCheck { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal NetProfit { get; set; }
        public decimal AverageCustomerValue { get; set; }
        public double ReturnPercentage { get; set; }
        public string MostPopularProduct { get; set; } = "Нет данных";
        public string LeastPopularProduct { get; set; } = "Нет данных";
    }

    public class AdminNewsEditViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? ExistingImageUrl { get; set; }
        public IFormFile? NewImage { get; set; }
    }

    public class AdminProductEditViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? ExistingImageUrl { get; set; }
        public IFormFile? NewImage { get; set; }

        public long ProductTypeId { get; set; }
        public List<ProductTypeModel> ProductTypes { get; set; } = new();

        [Required(ErrorMessage = "Введите цену")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше нуля")]
        [RegularExpression(@"^\d+([.,]\d{1,2})?$", ErrorMessage = "Введите корректную цену (до 2 знаков после запятой)")]
        [Display(Name = "Цена (₽)")]
        public decimal Price { get; set; }
    }
}