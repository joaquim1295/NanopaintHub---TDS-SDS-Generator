using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NanopaintHub.Infrastructure.Documents.Services;

/// <summary>Servidor HTTP local para servir quill-host e API de sessão do editor Chromium.</summary>
internal sealed class EditorAssetHttpServer : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpListener _listener = new();
    private readonly string _assetsDirectory;
    private readonly string _sessionId;
    private readonly string _initialHtml;
    private readonly TaskCompletionSource<string?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Task? _listenTask;
    private CancellationTokenSource? _listenCts;

    private EditorAssetHttpServer(
        string assetsDirectory,
        string sessionId,
        string initialHtml,
        int port,
        string hostPage)
    {
        _assetsDirectory = assetsDirectory;
        _sessionId = sessionId;
        _initialHtml = initialHtml ?? string.Empty;
        Port = port;
        EditorUrl = $"http://127.0.0.1:{port}/{hostPage}?mode=chromium&session={sessionId}";
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    }

    public int Port { get; }

    public string EditorUrl { get; }

    public static async Task<EditorAssetHttpServer> StartAsync(
        string assetsDirectory,
        string sessionId,
        string initialHtml,
        string hostPage,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(assetsDirectory))
        {
            throw new DirectoryNotFoundException($"Pasta de assets do editor não encontrada: {assetsDirectory}");
        }

        var port = GetFreePort();
        var server = new EditorAssetHttpServer(assetsDirectory, sessionId, initialHtml, port, hostPage);
        await server.StartListeningAsync(cancellationToken);
        return server;
    }

    public Task<string?> WaitForCompletionAsync(CancellationToken cancellationToken) =>
        _completion.Task.WaitAsync(cancellationToken);

    public void TryComplete(string? html) => _completion.TrySetResult(html);

    public void TryCancel() => _completion.TrySetResult(null);

    private async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        _listenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();
        _listenTask = Task.Run(() => ListenLoopAsync(_listenCts.Token), CancellationToken.None);
        await Task.Yield();
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                await HandleRequestAsync(context);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                if (context is not null)
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath.TrimEnd('/') ?? "/";
        if (path.Length == 0)
        {
            path = "/";
        }

        try
        {
            if (path.Equals("/api/html", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(response, new { html = _initialHtml, session = _sessionId });
                return;
            }

            if (path.Equals("/api/apply", StringComparison.OrdinalIgnoreCase) &&
                request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var body = await ReadBodyAsync(request);
                var payload = JsonSerializer.Deserialize<ApplyPayload>(body, JsonOptions);
                if (payload?.Session == _sessionId)
                {
                    TryComplete(payload.Html ?? string.Empty);
                }

                await WriteJsonAsync(response, new { ok = true });
                return;
            }

            if (path.Equals("/api/cancel", StringComparison.OrdinalIgnoreCase))
            {
                TryCancel();
                await WriteJsonAsync(response, new { ok = true });
                return;
            }

            if (path.Equals("/api/event", StringComparison.OrdinalIgnoreCase) &&
                request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(response, new { ok = true });
                return;
            }

            var relative = path.TrimStart('/');
            if (string.IsNullOrEmpty(relative))
            {
                relative = "quill-host.html";
            }

            var filePath = Path.GetFullPath(Path.Combine(_assetsDirectory, relative.Replace('/', Path.DirectorySeparatorChar)));
            var root = Path.GetFullPath(_assetsDirectory);
            if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
            {
                response.StatusCode = 404;
                response.Close();
                return;
            }

            var bytes = await File.ReadAllBytesAsync(filePath);
            response.ContentType = GetContentType(filePath);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
            response.Close();
        }
        catch
        {
            response.StatusCode = 500;
            response.Close();
        }
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        return await reader.ReadToEndAsync();
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private static string GetContentType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            _ => "application/octet-stream",
        };

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        _listenCts?.Cancel();
        _listener.Stop();
        if (_listenTask is not null)
        {
            try
            {
                await _listenTask;
            }
            catch
            {
                // ignore shutdown
            }
        }

        _listenCts?.Dispose();
        _listener.Close();
    }

    private sealed class ApplyPayload
    {
        public string? Session { get; set; }

        public string? Html { get; set; }
    }
}
