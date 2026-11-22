using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("cart_item")]
    public class CartItemModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("cart_id")]
        public long CartId { get; set; }

        [Column("product_id")]
        public long ProductId { get; set; }

        [Column("quantity")]
        public long Quantity { get; set; }

        [Column("is_coffee")]
        public bool IsCoffee { get; set; }

        [Reference(typeof(CartModel), includeInQuery: false)]
        public CartModel? Brand { get; set; }

        [Reference(typeof(ProductModel), includeInQuery: false)]
        public ProductModel? Product { get; set; }
    }
}
