namespace CapstoneProject.Commons;

public static class OrderDisplay
{
    public static string GetOrderTypeText(string orderType)
    {
        return orderType switch
        {
            "custom" => "Custom Design",
            "collection" => "Collection Design",
            "upload" => "Uploaded Design",
            _ => "-"
        };
    }

    public static string GetFabricText(string fabricName, string? fabricNote)
    {
        return string.IsNullOrWhiteSpace(fabricNote) ? fabricName : $"{fabricName} ({fabricNote})";
    }
}
