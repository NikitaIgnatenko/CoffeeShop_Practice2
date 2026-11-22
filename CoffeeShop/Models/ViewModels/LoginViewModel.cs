using System.ComponentModel.DataAnnotations;

namespace CoffeeShop.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введите номер телефона")]
        [Display(Name = "Номер телефона")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Введите пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = null!;
    }
}
