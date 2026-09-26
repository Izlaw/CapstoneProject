using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("conversations")]
public class ConversationModel : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;

    [Column("order_id")]
    public string? OrderId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "open";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
