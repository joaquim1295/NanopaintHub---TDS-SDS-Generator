namespace NanopaintHub.Application.Contracts;

/// <summary>
/// Editor visual de templates numa janela Chromium (Puppeteer headed).
/// </summary>
public interface IChromiumTinyMceEditorService
{
    /// <summary>
    /// Abre o editor na janela Chromium. Devolve HTML quando o utilizador clica «Aplicar na NanopaintHub».
    /// </summary>
    Task<ChromiumTinyMceEditorResult> OpenEditorAsync(
        string initialHtml,
        string editorAssetsDirectory,
        string cssTheme = "",
        ChromiumTemplateEditorMode mode = ChromiumTemplateEditorMode.WordLike,
        CancellationToken cancellationToken = default);
}

public sealed record ChromiumTinyMceEditorResult(bool Applied, string? Html, string Message);
