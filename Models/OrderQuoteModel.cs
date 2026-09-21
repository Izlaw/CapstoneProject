using Newtonsoft.Json;

namespace CapstoneProject.Models;

public class OrderItemRequestModel
{
    [JsonProperty("size_id")]
    public string SizeId { get; set; } = string.Empty;

    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}

public class OrderQuoteLineModel
{
    [JsonProperty("size_id")]
    public string SizeId { get; set; } = string.Empty;

    [JsonProperty("size_name")]
    public string SizeName { get; set; } = string.Empty;

    [JsonProperty("size_price")]
    public decimal SizePrice { get; set; }

    [JsonProperty("unit_price")]
    public decimal UnitPrice { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("line_total")]
    public decimal LineTotal { get; set; }
}

public class OrderQuoteModel
{
    [JsonProperty("order_type")]
    public string OrderType { get; set; } = string.Empty;

    [JsonProperty("fabric_id")]
    public string? FabricId { get; set; }

    [JsonProperty("fabric_name")]
    public string? FabricName { get; set; }

    [JsonProperty("custom_fabric_note")]
    public string? CustomFabricNote { get; set; }

    [JsonProperty("collection_id")]
    public string? CollectionId { get; set; }

    [JsonProperty("collection_name")]
    public string? CollectionName { get; set; }

    [JsonProperty("base_unit_price")]
    public decimal BaseUnitPrice { get; set; }

    [JsonProperty("timeframe_id")]
    public string? TimeframeId { get; set; }

    [JsonProperty("timeframe_label")]
    public string? TimeframeLabel { get; set; }

    [JsonProperty("surcharge_percent")]
    public decimal SurchargePercent { get; set; }

    [JsonProperty("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonProperty("surcharge_amount")]
    public decimal SurchargeAmount { get; set; }

    [JsonProperty("total")]
    public decimal Total { get; set; }

    [JsonProperty("total_quantity")]
    public int TotalQuantity { get; set; }

    [JsonProperty("lines")]
    public List<OrderQuoteLineModel> Lines { get; set; } = new();
}
