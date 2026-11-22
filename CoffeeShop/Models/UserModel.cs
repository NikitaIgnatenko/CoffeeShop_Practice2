using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("users")]
    public class UserModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("phone")]
        public string Phone { get; set; } = null!;

        [Column("pass_hash")]
        public string PassHash { get; set; } = null!;

        [Column("isadmin")]
        public bool IsAdmin { get; set; } = false;
    }
}
