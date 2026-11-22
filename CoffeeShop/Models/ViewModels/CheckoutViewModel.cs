namespace CoffeeShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public string UserName { get; set; } = null!;
        public string UserPhone { get; set; } = null!;
        public List<BranchModel> Branches { get; set; } = new();

        // Опционально: пусть пользователь выбирает, но валидацию в этом шаге не делаем
        public long? SelectedBranchId { get; set; }

        // --- Содержимое корзины ---
        public CartViewModel Cart { get; set; } = new CartViewModel();
    }
}