using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

[Table("messages")]
public class ChatMessageModel : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("conversation_id")]
    public string ConversationId { get; set; } = string.Empty;

    [Column("sender_id")]
    public string SenderId { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
