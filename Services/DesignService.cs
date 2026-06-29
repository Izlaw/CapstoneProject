using CapstoneProject.Models;

namespace CapstoneProject.Services;

public class DesignService
{
    public ShirtDesignModel CurrentDesign { get; private set; } = new();

    public event Action? OnDesignChanged;

    public void SetColor(string hexColor)
    {
        CurrentDesign.ShirtColor = hexColor;
        OnDesignChanged?.Invoke();
    }

    public void SetTextOverlay(string text, int fontSize, string color, string fontFamily)
    {
        CurrentDesign.TextOverlay = text;
        CurrentDesign.TextFontSize = fontSize;
        CurrentDesign.TextColor = color;
        CurrentDesign.TextFontFamily = fontFamily;
        OnDesignChanged?.Invoke();
    }

    public void SetLogo(string dataUrl)
    {
        CurrentDesign.HasLogo = !string.IsNullOrEmpty(dataUrl);
        CurrentDesign.LogoDataUrl = dataUrl;
        OnDesignChanged?.Invoke();
    }

    public void ClearDesign()
    {
        CurrentDesign.TextOverlay = null;
        CurrentDesign.HasLogo = false;
        CurrentDesign.LogoDataUrl = null;
        CurrentDesign.ShirtColor = "#ffffff";
        OnDesignChanged?.Invoke();
    }

    public void ResetToNew()
    {
        CurrentDesign = new ShirtDesignModel();
        OnDesignChanged?.Invoke();
    }
}
