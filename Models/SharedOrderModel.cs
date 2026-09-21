using Newtonsoft.Json;

namespace CapstoneProject.Models;

public class SharedOrderModel
{
    [JsonProperty("short_id")]
    public string ShortId { get; set; } = string.Empty;

    [JsonProperty("order_type")]
    public string OrderType { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("timeframe_label")]
    public string? TimeframeLabel { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("subtotal")]
    public decimal? Subtotal { get; set; }

    [JsonProperty("surcharge_percent")]
    public decimal? SurchargePercent { get; set; }

    [JsonProperty("surcharge_amount")]
    public decimal? SurchargeAmount { get; set; }

    [JsonProperty("total_price")]
    public decimal TotalPrice { get; set; }

    [JsonProperty("items")]
    public List<SharedOrderItemModel> Items { get; set; } = new();

    [JsonProperty("custom")]
    public SharedCustomOrderModel? Custom { get; set; }

    [JsonProperty("upload")]
    public SharedUploadOrderModel? Upload { get; set; }

    [JsonProperty("collection")]
    public SharedCollectionOrderModel? Collection { get; set; }
}

public class SharedOrderItemModel
{
    [JsonProperty("size_name")]
    public string SizeName { get; set; } = string.Empty;

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("unit_price")]
    public decimal UnitPrice { get; set; }

    [JsonProperty("line_total")]
    public decimal LineTotal { get; set; }
}

public class SharedCustomOrderModel
{
    [JsonProperty("fabric_name")]
    public string FabricName { get; set; } = string.Empty;

    [JsonProperty("fabric_note")]
    public string? FabricNote { get; set; }

    [JsonProperty("shirt_color")]
    public string ShirtColor { get; set; } = string.Empty;

    [JsonProperty("design_data")]
    public DesignDataModel DesignData { get; set; } = new();
}

public class SharedUploadOrderModel
{
    [JsonProperty("fabric_name")]
    public string FabricName { get; set; } = string.Empty;

    [JsonProperty("fabric_note")]
    public string? FabricNote { get; set; }

    [JsonProperty("original_file_name")]
    public string? OriginalFileName { get; set; }

    [JsonProperty("image_url")]
    public string? ImageUrl { get; set; }
}

public class SharedCollectionOrderModel
{
    [JsonProperty("collection_name")]
    public string CollectionName { get; set; } = string.Empty;
}
