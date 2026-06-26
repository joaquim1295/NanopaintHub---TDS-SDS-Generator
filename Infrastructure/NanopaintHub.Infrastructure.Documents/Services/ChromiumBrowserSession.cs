using PuppeteerSharp;

namespace NanopaintHub.Infrastructure.Documents.Services;

/// <summary>Chromium partilhado entre geração PDF e preview HTML.</summary>
internal static class ChromiumBrowserSession
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static bool _browserDownloaded;

    public static async Task<T> RunAsync<T>(
        Func<IBrowser, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (!_browserDownloaded)
            {
                await Task.Run(async () => await ChromiumBrowserBootstrap.EnsureDownloadedAsync(cancellationToken));
                _browserDownloaded = true;
            }

            await using var browser = await Task.Run(async () => await ChromiumBrowserBootstrap.LaunchAsync(
                headless: true,
                cancellationToken: cancellationToken));

            return await action(browser);
        }
        finally
        {
            Gate.Release();
        }
    }

    public static string WrapBodyHtml(string bodyHtml, int topMarginPx, int bottomMarginPx)
    {
        var safeBody = bodyHtml ?? string.Empty;
        return $$"""
<!DOCTYPE html>
<html lang="pt">
<head>
  <meta charset="utf-8" />
  <style>
    @page { size: A4; margin: {{topMarginPx}}px 20px {{bottomMarginPx}}px 20px; }
    :root { color-scheme: light; }
    html, body { background: #fff; }
    body { font-family: "Segoe UI", Arial, sans-serif; margin: 0; padding: 24px; color: #111; }
  </style>
</head>
<body>
{{safeBody}}
</body>
</html>
""";
    }
}
