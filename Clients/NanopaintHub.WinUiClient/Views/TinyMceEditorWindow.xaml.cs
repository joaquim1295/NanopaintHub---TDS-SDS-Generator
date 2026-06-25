using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class TinyMceEditorWindow : Window
{
    private readonly string _initialHtml;
    private readonly string _assetsPath;
    private readonly string _cssTheme;
    private readonly TaskCompletionSource<string?> _tcs;

    public TinyMceEditorWindow(string initialHtml, string assetsPath, string cssTheme)
    {
        InitializeComponent();
        
        _initialHtml = initialHtml ?? string.Empty;
        _assetsPath = assetsPath;
        _cssTheme = cssTheme ?? string.Empty;
        _tcs = new TaskCompletionSource<string?>();

        this.Closed += (s, e) => 
        {
            _tcs.TrySetResult(null);
        };

        _ = InitializeAsync();
    }

    public Task<string?> WaitForResultAsync() => _tcs.Task;

    private async Task InitializeAsync()
    {
        try
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub", "WebView2");
            Directory.CreateDirectory(appData);
            
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", appData);
            await EditorWebView.EnsureCoreWebView2Async();

            EditorWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("local.assets", _assetsPath, CoreWebView2HostResourceAccessKind.Allow);

            EditorWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            EditorWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            StatusText.Text = "Carregando TinyMCE...";
            EditorWebView.CoreWebView2.Navigate("http://local.assets/wysiwyg-host.html");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erro ao inicializar WebView2: {ex.Message}";
        }
    }

    private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.IsSuccess)
        {
            StatusText.Text = "Pronto";
            StatusText.Visibility = Visibility.Collapsed;
        }
        else
        {
            StatusText.Text = $"Erro de navegação: {args.WebErrorStatus}";
        }
    }

    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var msg = args.WebMessageAsJson;
            if (string.IsNullOrEmpty(msg)) return;

            var payload = JsonSerializer.Deserialize<JsonElement>(msg);
            var type = payload.GetProperty("type").GetString();

            if (type == "editor-ready")
            {
                var initData = new
                {
                    type = "init-data",
                    html = _initialHtml,
                    cssTheme = _cssTheme
                };
                EditorWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(initData));
            }
            else if (type == "apply")
            {
                var html = payload.GetProperty("html").GetString();
                _tcs.TrySetResult(html);
                this.Close();
            }
            else if (type == "cancel")
            {
                _tcs.TrySetResult(null);
                this.Close();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing WebMessage: {ex}");
        }
    }
}
