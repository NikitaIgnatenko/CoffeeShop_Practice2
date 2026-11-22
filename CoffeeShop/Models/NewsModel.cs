using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("news")]
    public class NewsModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("title")]
        public string Title { get; set; } = null!;

        [Column("description")]
        public string Description { get; set; } = null!;

        [Column("img_url")]
        public string ImageUrl { get; set; } = null!;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
