using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("orders")]
public class AppOrderModel : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;

    [Column("order_type")]
    public string OrderType { get; set; } = "custom"; // custom, upload, collection

    [Column("status")]
    public string Status { get; set; } = "Pending"; // Pending, In Progress, Ready for Pickup, Completed, Cancelled

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("base_unit_price")]
    public decimal? BaseUnitPrice { get; set; }

    [Column("timeframe_id")]
    public string? TimeframeId { get; set; }

    [Column("timeframe_label")]
    public string? TimeframeLabel { get; set; }

    [Column("surcharge_percent")]
    public decimal? SurchargePercent { get; set; }

    [Column("subtotal")]
    public decimal? Subtotal { get; set; }

    [Column("surcharge_amount")]
    public decimal? SurchargeAmount { get; set; }

    [Column("share_token", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public string ShareToken { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime? UpdatedAt { get; set; }

    [Reference(typeof(CustomOrderModel), ReferenceAttribute.JoinType.Left, includeInQuery: false)]
    public CustomOrderModel? CustomOrder { get; set; }

    [Reference(typeof(CollectionOrderModel), ReferenceAttribute.JoinType.Left, includeInQuery: false)]
    public CollectionOrderModel? CollectionOrder { get; set; }

    [Reference(typeof(UploadOrderModel), ReferenceAttribute.JoinType.Left, includeInQuery: false)]
    public UploadOrderModel? UploadOrder { get; set; }

    [Reference(typeof(OrderItemModel), ReferenceAttribute.JoinType.Left, includeInQuery: false)]
    public List<OrderItemModel> Items { get; set; } = new();
}
