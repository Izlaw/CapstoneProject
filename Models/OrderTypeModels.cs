using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CapstoneProject.Models;

public class DesignElementModel
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }

    [JsonProperty("font", NullValueHandling = NullValueHandling.Ignore)]
    public string? Font { get; set; }

    [JsonProperty("fontSize", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? FontSize { get; set; }

    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
    public string? Color { get; set; }

    [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? Size { get; set; }

    [JsonProperty("originX", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? OriginX { get; set; }

    [JsonProperty("originY", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? OriginY { get; set; }

    [JsonProperty("originZ", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? OriginZ { get; set; }

    [JsonProperty("dirX", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? DirX { get; set; }

    [JsonProperty("dirY", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? DirY { get; set; }

    [JsonProperty("dirZ", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? DirZ { get; set; }

    [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
    public string? Path { get; set; }

    [JsonProperty("dataUrl", NullValueHandling = NullValueHandling.Ignore)]
    public string? DataUrl { get; set; }
}

public class DesignDataModel
{
    [JsonProperty("elements")]
    public List<DesignElementModel> Elements { get; set; } = new();
}

[Table("custom_orders")]
public class CustomOrderModel : BaseModel
{
    [PrimaryKey("order_id", true)]
    public string OrderId { get; set; } = string.Empty;

    [Column("fabric_id")]
    public string FabricId { get; set; } = string.Empty;

    [Column("fabric_name")]
    public string FabricName { get; set; } = string.Empty;

    [Column("fabric_note")]
    public string? FabricNote { get; set; }

    [Column("shirt_color")]
    public string ShirtColor { get; set; } = string.Empty;

    [Column("design_data")]
    public DesignDataModel DesignData { get; set; } = new();

    [Column("design_image_path")]
    public string? DesignImagePath { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}

[Table("upload_orders")]
public class UploadOrderModel : BaseModel
{
    [PrimaryKey("order_id", true)]
    public string OrderId { get; set; } = string.Empty;

    [Column("fabric_id")]
    public string FabricId { get; set; } = string.Empty;

    [Column("fabric_name")]
    public string FabricName { get; set; } = string.Empty;

    [Column("fabric_note")]
    public string? FabricNote { get; set; }

    [Column("uploaded_image_path")]
    public string UploadedImagePath { get; set; } = string.Empty;

    [Column("original_file_name")]
    public string? OriginalFileName { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}

[Table("collection_orders")]
public class CollectionOrderModel : BaseModel
{
    [PrimaryKey("order_id", true)]
    public string OrderId { get; set; } = string.Empty;

    [Column("collection_id")]
    public string CollectionId { get; set; } = string.Empty;

    [Column("collection_name")]
    public string CollectionName { get; set; } = string.Empty;

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}
