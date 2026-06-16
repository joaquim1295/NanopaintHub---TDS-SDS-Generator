using NanopaintHub.Application.Contracts;
using PuppeteerSharp;

namespace NanopaintHub.Infrastructure.Documents.Services;

public sealed class ChromiumTinyMceEditorService : IChromiumTinyMceEditorService
{
    private static readonly SemaphoreSlim EditorGate = new(1, 1);

    public static string ResolveHostPage(ChromiumTemplateEditorMode mode) =>
        mode == ChromiumTemplateEditorMode.QuillSimple
            ? "quill-host.html"
            : "wysiwyg-host.html";

    public async Task<ChromiumTinyMceEditorResult> OpenEditorAsync(
        string initialHtml,
        string editorAssetsDirectory,
        string cssTheme = "",
        ChromiumTemplateEditorMode mode = ChromiumTemplateEditorMode.WordLike,
        CancellationToken cancellationToken = default)
    {
        await EditorGate.WaitAsync(cancellationToken);
        IBrowser? browser = null;
        var hostPagePath = Path.Combine(editorAssetsDirectory, ResolveHostPage(mode));
        var appUrl = "file:///" + hostPagePath.Replace('\\', '/');

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await ChromiumBrowserBootstrap.EnsureDownloadedAsync(cancellationToken);

            browser = await ChromiumBrowserBootstrap.LaunchAsync(
                headless: false,
                extraArgs:
                [
                    "--app=data:text/html,<html><head><title>A carregar NanopaintHub Editor...</title></head><body style=\"background-color:%231f2937\"></body></html>",
                    "--window-size=1024,768",
                    "--disable-web-security",
                    "--allow-file-access-from-files"
                ],
                cancellationToken);

            browser.Disconnected += (_, _) => tcs.TrySetResult(null);

            var pages = await browser.PagesAsync();
            var page = pages.FirstOrDefault() ?? await browser.NewPageAsync();

            await page.ExposeFunctionAsync("getInitialData", new Func<object>(() => new { html = initialHtml ?? string.Empty, cssTheme = cssTheme ?? string.Empty }));
            await page.ExposeFunctionAsync("applyChanges", new Func<string, bool>(html => { tcs.TrySetResult(html); return true; }));
            await page.ExposeFunctionAsync("cancelEdit", new Func<bool>(() => { tcs.TrySetResult(null); return true; }));

            // Always navigate explicitly to ensure functions are exposed beforehand
            await page.GoToAsync($"{appUrl}?mode=chromium");

            var html = await tcs.Task.WaitAsync(cancellationToken);
            if (html is not null)
            {
                return new ChromiumTinyMceEditorResult(true, html, "Conteúdo aplicado na app.");
            }

            return new ChromiumTinyMceEditorResult(false, null, "Edição cancelada ou janela fechada.");
        }
        catch (OperationCanceledException)
        {
            return new ChromiumTinyMceEditorResult(false, null, "Edição cancelada.");
        }
        catch (Exception ex)
        {
            return new ChromiumTinyMceEditorResult(false, null, $"Editor Chromium indisponível: {ex.Message}");
        }
        finally
        {
            if (browser is not null)
            {
                try
                {
                    await browser.CloseAsync();
                }
                catch
                {
                    // ignore close errors
                }
            }

            EditorGate.Release();
        }
    }
}
