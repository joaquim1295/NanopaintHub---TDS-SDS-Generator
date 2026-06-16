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
    public string ResultHtml { get; private set; } = "";
    private bool _isReady = false;

    // Use a TaskCompletionSource to await the window close
    private TaskCompletionSource<bool> _tcs = new();

    public TinyMceEditorWindow(string initialHtml)
    {
        this.InitializeComponent();
        _initialHtml = initialHtml;
        
        // Maximize the window
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
        var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
        presenter?.Maximize();

        var rootGrid = this.Content as FrameworkElement;
        if (rootGrid != null)
        {
            rootGrid.Loaded += TinyMceEditorWindow_Loaded;
        }
        
        this.Closed += (s, e) => { _tcs.TrySetResult(false); };
    }

    public Task<bool> ShowAsync()
    {
        this.Activate();
        return _tcs.Task;
    }

    private async void TinyMceEditorWindow_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub", "WebView2", "TinyMCE");
            Directory.CreateDirectory(userDataFolder);
            
            var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions());
            await EditorWebView.EnsureCoreWebView2Async(env);
            
            EditorWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
            EditorWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets.local", assetsPath, CoreWebView2HostResourceAccessKind.Allow);

            EditorWebView.CoreWebView2.Navigate("http://appassets.local/tinymce.html");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView2 Error: {ex.Message}");
        }
    }

    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var msg = args.TryGetWebMessageAsString();
            if (msg.Contains("\"type\":\"READY\"") || msg.Contains("\"type\": \"READY\""))
            {
                _isReady = true;
                InjectInitialHtml();
            }
        }
        catch { }
    }

    private async void InjectInitialHtml()
    {
        if (_isReady && EditorWebView.CoreWebView2 != null)
        {
            // Escape HTML for JS string literal
            var jsonString = JsonSerializer.Serialize(_initialHtml);
            await EditorWebView.ExecuteScriptAsync($"setHtmlContent({jsonString});");
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReady && EditorWebView.CoreWebView2 != null)
        {
            var result = await EditorWebView.ExecuteScriptAsync("getHtmlContent();");
            if (result != null && result != "null")
            {
                ResultHtml = JsonSerializer.Deserialize<string>(result) ?? "";
                _tcs.TrySetResult(true);
            }
        }
        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _tcs.TrySetResult(false);
        this.Close();
    }
}
