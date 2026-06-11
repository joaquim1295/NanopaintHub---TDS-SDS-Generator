namespace NanopaintHub.Application.Contracts;

/// <summary>
/// Editor visual de templates numa janela Chromium (Puppeteer headed).
/// </summary>
public interface IChromiumQuillEditorService
{
    /// <summary>
    /// Abre o editor na janela Chromium. Devolve HTML quando o utilizador clica «Aplicar na NanopaintHub».
    /// </summary>
    Task<ChromiumQuillEditorResult> OpenEditorAsync(
        string initialHtml,
        string editorAssetsDirectory,
        ChromiumTemplateEditorMode mode = ChromiumTemplateEditorMode.WordLike,
        CancellationToken cancellationToken = default);
}

public sealed record ChromiumQuillEditorResult(bool Applied, string? Html, string Message);
