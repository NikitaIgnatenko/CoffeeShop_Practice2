using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("coffee_sticker")]
    public class CoffeeStickerModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("cart_item_id")]
        public long CartItemId { get; set; }

        [Column("sticker_id")]
        public long StickerId { get; set; }

        [Column("x")]
        public int x { get; set; }

        [Column("y")]
        public int y { get; set; }

        [Reference(typeof(CartItemModel), includeInQuery: false)]
        public CartItemModel? CartItem { get; set; }

        [Reference(typeof(StickerModel), includeInQuery: false)]
        public StickerModel? Sticker { get; set; }
    }
}
