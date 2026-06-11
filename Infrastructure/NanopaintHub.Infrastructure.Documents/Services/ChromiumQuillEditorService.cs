using NanopaintHub.Application.Contracts;
using PuppeteerSharp;

namespace NanopaintHub.Infrastructure.Documents.Services;

public sealed class ChromiumQuillEditorService : IChromiumQuillEditorService
{
    private static readonly SemaphoreSlim EditorGate = new(1, 1);

    public static string ResolveHostPage(ChromiumTemplateEditorMode mode) =>
        mode == ChromiumTemplateEditorMode.QuillSimple
            ? "quill-host.html"
            : "wysiwyg-host.html";

    public async Task<ChromiumQuillEditorResult> OpenEditorAsync(
        string initialHtml,
        string editorAssetsDirectory,
        ChromiumTemplateEditorMode mode = ChromiumTemplateEditorMode.WordLike,
        CancellationToken cancellationToken = default)
    {
        await EditorGate.WaitAsync(cancellationToken);
        IBrowser? browser = null;
        var hostPage = ResolveHostPage(mode);

        await using var server = await EditorAssetHttpServer.StartAsync(
            editorAssetsDirectory,
            Guid.NewGuid().ToString("N"),
            initialHtml ?? string.Empty,
            hostPage,
            cancellationToken);

        try
        {
            await ChromiumBrowserBootstrap.EnsureDownloadedAsync(cancellationToken);

            browser = await ChromiumBrowserBootstrap.LaunchAsync(
                headless: false,
                extraArgs:
                [
                    $"--app={server.EditorUrl}",
                    "--window-size=1024,768",
                ],
                cancellationToken);

            browser.Disconnected += (_, _) => server.TryCancel();

            var html = await server.WaitForCompletionAsync(cancellationToken);
            if (html is not null)
            {
                return new ChromiumQuillEditorResult(true, html, "Conteúdo aplicado na app.");
            }

            return new ChromiumQuillEditorResult(false, null, "Edição cancelada ou janela fechada.");
        }
        catch (OperationCanceledException)
        {
            return new ChromiumQuillEditorResult(false, null, "Edição cancelada.");
        }
        catch (Exception ex)
        {
            return new ChromiumQuillEditorResult(false, null, $"Editor Chromium indisponível: {ex.Message}");
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
