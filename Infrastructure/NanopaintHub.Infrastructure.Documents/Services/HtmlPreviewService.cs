using NanopaintHub.Application.Contracts;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace NanopaintHub.Infrastructure.Documents.Services;

public sealed class HtmlPreviewService : IHtmlPreviewService
{
    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(60);

    public Task<byte[]> RenderPreviewPngAsync(
        string bodyHtml,
        int topMarginPx = 0,
        int bottomMarginPx = 0,
        CancellationToken cancellationToken = default) =>
        ChromiumBrowserSession.RunAsync(
            async browser =>
            {
                await using var page = await browser.NewPageAsync();
                var doc = ChromiumBrowserSession.WrapBodyHtml(bodyHtml, topMarginPx, bottomMarginPx);
                await page.SetContentAsync(doc);
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 794,
                    Height = 1123,
                    DeviceScaleFactor = 1,
                });

                var screenshotTask = page.ScreenshotDataAsync(new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    FullPage = true,
                });

                var completed = await Task.WhenAny(screenshotTask, Task.Delay(RenderTimeout, cancellationToken));
                if (completed != screenshotTask)
                {
                    throw new TimeoutException("Renderização do preview excedeu 60 segundos.");
                }

                return await screenshotTask;
            },
            cancellationToken);
}
