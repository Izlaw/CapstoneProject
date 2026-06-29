namespace CapstoneProject.Models;

public class ShirtDesign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "My Design";
    public string ShirtColor { get; set; } = "#ffffff";
    public string? TextOverlay { get; set; }
    public int TextFontSize { get; set; } = 36;
    public string TextColor { get; set; } = "#000000";
    public string TextFontFamily { get; set; } = "Arial";
    public bool HasLogo { get; set; } = false;
    public string? LogoDataUrl { get; set; }
    public string? TextureUrl { get; set; }  // Supabase Storage URL after save
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
