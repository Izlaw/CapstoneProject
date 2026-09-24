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
}
