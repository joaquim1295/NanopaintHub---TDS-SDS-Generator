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
        // Edge is already installed on Windows, no need to download Chromium
        // We bypass BrowserFetcher entirely.
        await Task.CompletedTask;
    }

    public static async Task<IBrowser> LaunchAsync(
        bool headless,
        string[]? extraArgs = null,
        CancellationToken cancellationToken = default)
    {
        var edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        if (!System.IO.File.Exists(edgePath))
        {
            throw new FileNotFoundException("Microsoft Edge não foi encontrado neste sistema. Por favor instale o Edge ou verifique o caminho.");
        }

        var options = new LaunchOptions
        {
            Headless = headless,
            ExecutablePath = edgePath,
            Args = extraArgs ?? Array.Empty<string>(),
            DefaultViewport = null
        };

        var launch = Puppeteer.LaunchAsync(options);

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
