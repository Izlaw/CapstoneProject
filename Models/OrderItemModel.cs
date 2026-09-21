using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("order_items")]
public class OrderItemModel : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [Column("size_id")]
    public string SizeId { get; set; } = string.Empty;

    [Column("size_name")]
    public string SizeName { get; set; } = string.Empty;

    [Column("size_price")]
    public decimal SizePrice { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("line_total")]
    public decimal LineTotal { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}
