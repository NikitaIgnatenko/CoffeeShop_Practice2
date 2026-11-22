using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("stickers")]
    public class StickerModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("img_url")]
        public string ImageUrl { get; set; } = null!;
    }
}
