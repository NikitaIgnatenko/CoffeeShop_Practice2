namespace CoffeeShop.Models.ViewModels
{
    public class CatalogViewModel
    {
        public IReadOnlyList<ProductTypeModel> ProductTypes { get; set; } = Array.Empty<ProductTypeModel>();
        public IReadOnlyList<ProductModel> Products { get; set; } = Array.Empty<ProductModel>();
        public Dictionary<long, long> CartQuantities { get; set; } = new();
        public long? SelectedProductTypeId { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 18;
        public int TotalPages { get; set; }
        public long TotalCount { get; set; }
    }
}
