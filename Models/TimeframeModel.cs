using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("timeframes")]
public class TimeframeModel : BaseModel, ISortableItem
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("label")]
    public string Label { get; set; } = string.Empty;

    [Column("surcharge_percent")]
    public decimal SurchargePercent { get; set; }

    [Column("max_quantity", NullValueHandling.Include)]
    public int? MaxQuantity { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime UpdatedAt { get; set; }
}
