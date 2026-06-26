using PuppeteerSharp;

namespace NanopaintHub.Infrastructure.Documents.Services;

/// <summary>Download e arranque partilhado do Chromium Puppeteer (headless e headed).</summary>
internal static class ChromiumBrowserBootstrap
{
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(8);
    private static readonly TimeSpan BootTimeout = TimeSpan.FromSeconds(90);
    private static readonly SemaphoreSlim DownloadGate = new(1, 1);
    private static bool _browserDownloaded;

    public static async Task EnsureDownloadedAsync(CancellationToken cancellationToken = default)
    {
        await DownloadGate.WaitAsync(cancellationToken);
        try
        {
            if (_browserDownloaded)
            {
                return;
            }

            var fetcher = new BrowserFetcher();
            await WithTimeoutAsync(
                fetcher.DownloadAsync(),
                DownloadTimeout,
                "Download do Chromium excedeu o tempo limite.",
                cancellationToken);
            _browserDownloaded = true;
        }
        finally
        {
            DownloadGate.Release();
        }
    }

    public static async Task<IBrowser> LaunchAsync(
        bool headless,
        IReadOnlyList<string>? extraArgs = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureDownloadedAsync(cancellationToken);

        var args = extraArgs?.ToArray();
        var launch = Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = headless,
            DefaultViewport = headless ? new ViewPortOptions { Width = 794, Height = 1123 } : null,
            Args = args is { Length: > 0 } ? args : null,
        });

        return await WithTimeoutAsync(
            launch,
            BootTimeout,
            "Inicialização do Chromium excedeu o tempo limite.",
            cancellationToken);
    }

    private static async Task WithTimeoutAsync(
        Task task,
        TimeSpan timeout,
        string message,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            await task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(message);
        }
    }

    private static async Task<T> WithTimeoutAsync<T>(
        Task<T> task,
        TimeSpan timeout,
        string message,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            return await task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(message);
        }
    }
}
