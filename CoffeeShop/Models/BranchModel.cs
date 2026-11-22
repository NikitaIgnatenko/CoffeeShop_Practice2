using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("branch")]
    public class BranchModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("address")]
        public string Address { get; set; } = null!;

    }
}
