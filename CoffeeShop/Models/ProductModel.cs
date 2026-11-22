using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace CoffeeShop.Models
{
    [Table("products")]
    public class ProductModel : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("description")]
        public string Description { get; set; } = null!;

        [Column("price")]
        public decimal Price { get; set; }

        [Column("img_url")]
        public string ImageUrl { get; set; } = null!;

        [Column("product_type_id")]
        public long ProductTypeId { get; set; }

        [Reference(typeof(ProductTypeModel), includeInQuery: false)]
        public ProductTypeModel? ProductType { get; set; }
    }
}
