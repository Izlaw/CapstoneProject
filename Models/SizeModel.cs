using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("sizes")]
public class SizeModel : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("width_in", NullValueHandling.Include)]
    public decimal? WidthIn { get; set; }

    [Column("length_in", NullValueHandling.Include)]
    public decimal? LengthIn { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime UpdatedAt { get; set; }
}
