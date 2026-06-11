using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace WebView2.WinUiSmoke;

public sealed partial class MainWindow : Window
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NanopaintHub",
        "Logs",
        "wv2-smoke-winui.log");

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        Log("Smoke WinUI arrancou.");
        SmokeWebView.Loaded += async (_, _) => await RunEnsureAsync();
    }

    private async void EnsureButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RunEnsureAsync();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    private async Task RunEnsureAsync()
    {
        EnsureButton.IsEnabled = false;
        try
        {
            Log("Ensure início...");
            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NanopaintHub",
                "WebView2",
                "smoke-winui",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(userData);
            var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, userData, new CoreWebView2EnvironmentOptions());
            await SmokeWebView.EnsureCoreWebView2Async(env);
            SmokeWebView.CoreWebView2!.Navigate("https://example.com");
            Log($"Ensure OK. CoreWebView2={SmokeWebView.CoreWebView2 is not null}. Version={env.BrowserVersionString}");
        }
        catch (Exception ex)
        {
            Log($"Ensure FALHOU: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            EnsureButton.IsEnabled = true;
        }
    }

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        File.AppendAllText(LogPath, line + Environment.NewLine);
    }
}
