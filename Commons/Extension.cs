using CapstoneProject.Models;
using MudBlazor;

namespace CapstoneProject.Commons;

public static class Extension
{
    public static void ShowAlert(string message, Variant variant, ISnackbar snackbarService, Severity severityType)
    {
        snackbarService.Clear();
        snackbarService.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;
        snackbarService.Configuration.SnackbarVariant = variant;
        snackbarService.Configuration.MaxDisplayedSnackbars = 10;
        snackbarService.Configuration.VisibleStateDuration = 2000;
        snackbarService.Add($"{message}", severityType);
    }

    public static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!url.StartsWith('/')) return false;
        if (url.StartsWith("//") || url.StartsWith("/\\")) return false;

        return true;
    }

    public static DateTime ToDisplayTime(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Local);
    }

    public static bool MatchesSearch(string? value, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return true;
        if (string.IsNullOrEmpty(value)) return false;

        return value.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryReadDataUrl(string? dataUrl, out byte[] bytes, out string contentType)
    {
        bytes = Array.Empty<byte>();
        contentType = string.Empty;

        if (string.IsNullOrEmpty(dataUrl) || !dataUrl.StartsWith("data:")) return false;

        var separatorIndex = dataUrl.IndexOf(";base64,", StringComparison.Ordinal);
        if (separatorIndex < 0) return false;

        contentType = dataUrl.Substring(5, separatorIndex - 5);

        try
        {
            bytes = Convert.FromBase64String(dataUrl.Substring(separatorIndex + ";base64,".Length));
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool MatchesStatus(bool isActive, StatusFilter status)
    {
        return status switch
        {
            StatusFilter.Active => isActive,
            StatusFilter.Inactive => !isActive,
            _ => true
        };
    }
}
