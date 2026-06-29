using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("profiles")]
public class UserProfileModel : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("role")]
    public string Role { get; set; } = "customer";

    [Column("address")]
    public string? Address { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
