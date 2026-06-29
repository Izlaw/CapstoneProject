using Microsoft.JSInterop;

namespace CapstoneProject.Services;

public class ExportService
{
    private readonly IJSRuntime _js;

    public ExportService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task DownloadPngAsync(IJSObjectReference designerModule)
    {
        var dataUrl = await designerModule.InvokeAsync<string>("exportPng");
        if (string.IsNullOrEmpty(dataUrl)) return;

        // Trigger browser download via interop
        await _js.InvokeVoidAsync("downloadDataUrl", dataUrl, $"threadstudio-design-{DateTime.Now:yyyyMMdd-HHmmss}.png");
    }
}
