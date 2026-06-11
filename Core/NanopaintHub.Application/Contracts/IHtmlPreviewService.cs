namespace NanopaintHub.Application.Contracts;

/// <summary>Renderiza HTML para imagem via Chromium (Puppeteer), alinhado com o pipeline PDF.</summary>
public interface IHtmlPreviewService
{
    Task<byte[]> RenderPreviewPngAsync(
        string bodyHtml,
        int topMarginPx = 0,
        int bottomMarginPx = 0,
        CancellationToken cancellationToken = default);
}
