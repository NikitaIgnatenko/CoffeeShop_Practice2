using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("orders")]
    public class OrderModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("users_id")]
        public long UserId { get; set; }

        [Column("cart_id")]
        public long CartId { get; set; }

        [Column("branch_id")]
        public long BranchId { get; set; }

        [Column("final_price")]
        public decimal FinalPrice { get; set; }

        [Reference(typeof(UserModel), includeInQuery: false)]
        public UserModel? user { get; set; }

        [Reference(typeof(CartModel), includeInQuery: false)]
        public CartModel? Cart { get; set; }

        [Reference(typeof(BranchModel), includeInQuery: false)]
        public BranchModel? Branch { get; set; }
    }
}
