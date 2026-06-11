using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WebView2Smoke.WinForms;

internal sealed class SmokeForm : Form
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NanopaintHub",
        "Logs",
        "wv2-smoke-winforms.log");

    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly Button _ensureButton = new() { Text = "Ensure + Navigate", Dock = DockStyle.Top, Height = 36 };

    public SmokeForm()
    {
        Text = "WebView2 WinForms Smoke Test";
        Width = 900;
        Height = 700;
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        Log("Smoke WinForms arrancou.");

        Controls.Add(_webView);
        Controls.Add(_ensureButton);
        _ensureButton.Click += async (_, _) => await RunEnsureAsync();
        Shown += async (_, _) => await RunEnsureAsync();
    }

    private async Task RunEnsureAsync()
    {
        _ensureButton.Enabled = false;
        try
        {
            Log("Ensure início...");
            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NanopaintHub",
                "WebView2",
                "smoke-winforms",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(userData);
            var env = await CoreWebView2Environment.CreateAsync(null, userData, new CoreWebView2EnvironmentOptions());
            await _webView.EnsureCoreWebView2Async(env);
            _webView.CoreWebView2!.Navigate("https://example.com");
            Log($"Ensure OK. CoreWebView2={_webView.CoreWebView2 is not null}. Version={env.BrowserVersionString}");
        }
        catch (Exception ex)
        {
            Log($"Ensure FALHOU: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _ensureButton.Enabled = true;
        }
    }

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        File.AppendAllText(LogPath, line + Environment.NewLine);
    }
}
