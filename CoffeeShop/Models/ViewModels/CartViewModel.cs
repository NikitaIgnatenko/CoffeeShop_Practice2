namespace CoffeeShop.Models.ViewModels
{
    public class CartItemViewModel
    {
        public long ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public decimal UnitPrice { get; set; } 
        public long Quantity { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);
        public bool IsEmpty => Items.Count == 0;
        public string? Message { get; set; }
    }
}