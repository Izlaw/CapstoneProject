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

    [Column("fabric_type")]
    public string? FabricType { get; set; }

    [Column("size")]
    public string? Size { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("timeframe")]
    public string Timeframe { get; set; } = "Standard"; // Standard, Rush

    [Column("design_reference")]
    public string? DesignReference { get; set; } // URL to saved design PNG or Collection Item ID

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
