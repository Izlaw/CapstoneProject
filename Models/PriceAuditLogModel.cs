using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("price_audit_log")]
public class PriceAuditLogModel : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("table_name")]
    public string SourceTable { get; set; } = string.Empty;

    [Column("row_id")]
    public string RowId { get; set; } = string.Empty;

    [Column("row_label")]
    public string? RowLabel { get; set; }

    [Column("field")]
    public string Field { get; set; } = string.Empty;

    [Column("old_value")]
    public string? OldValue { get; set; }

    [Column("new_value")]
    public string? NewValue { get; set; }

    [Column("changed_by")]
    public string? ChangedBy { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; }
}
