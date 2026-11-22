using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("cart")]
    public class CartModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("users_id")]
        public long UsersId { get; set; }

        [Column("isBlocked")]
        public bool IsBlocked { get; set; }

        [Reference(typeof(UserModel), includeInQuery: false)]
        public UserModel? user { get; set; }
    }
}
