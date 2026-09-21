namespace CapstoneProject.Models;

public class OrderRequestModel
{
    public string OrderType { get; set; } = string.Empty;
    public List<OrderItemRequestModel> Items { get; set; } = new();
    public string? FabricId { get; set; }
    public string? FabricNote { get; set; }
    public string? CollectionId { get; set; }
    public string? TimeframeId { get; set; }
    public string? ShirtColor { get; set; }
    public DesignDataModel? DesignData { get; set; }
    public string? ImagePath { get; set; }
    public string? OriginalFileName { get; set; }
}
