using NanopaintHub_WinUIClient.Services;
using Xunit;

namespace NanopaintHub.UnitTests.WebView2;

/// <summary>Testes estáticos do pipeline de diagnóstico WebView2 (sem controlo UI).</summary>
public class WebView2DiagnosticsTests
{
    [Fact]
    public void Trace_log_path_is_under_local_app_data_or_workspace()
    {
        Assert.False(string.IsNullOrWhiteSpace(WebView2Trace.LogFilePath));
        Assert.Contains("webview2-trace.log", WebView2Trace.LogFilePath);
    }

    [Fact]
    public void Environment_context_includes_packaged_flag_and_runtime_version()
    {
        var ctx = WebView2Trace.GetEnvironmentContext();
        Assert.Contains("Packaged=", ctx);
        Assert.Contains("RuntimeVersion=", ctx);
    }

    [Fact]
    public void Browser_locator_reports_runtime_when_edge_webview_installed()
    {
        var available = WebView2BrowserLocator.IsRuntimeAvailable();
        var version = WebView2Trace.TryGetBrowserVersionString();
        if (available)
        {
            Assert.DoesNotContain("(vazio)", version);
            Assert.DoesNotContain("(erro:", version);
        }
    }

    [Fact]
    public void Evergreen_folder_when_found_contains_msedgewebview2()
    {
        var folder = WebView2BrowserLocator.TryResolveEvergreenBrowserFolder();
        if (folder is null)
        {
            return;
        }

        var hasBinary =
            File.Exists(Path.Combine(folder, "msedgewebview2.exe")) ||
            File.Exists(Path.Combine(folder, "msedge.exe"));
        Assert.True(hasBinary, $"Pasta Evergreen inválida: {folder}");
    }

    [Fact]
    public void Shared_user_data_folder_is_under_nanopaint_hub()
    {
        var path = WebView2SharedEnvironmentService.SharedUserDataFolder;
        Assert.Contains("NanopaintHub", path);
        Assert.Contains("WebView2", path);
    }
}
