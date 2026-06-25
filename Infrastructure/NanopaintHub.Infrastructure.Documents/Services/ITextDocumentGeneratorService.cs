namespace NanopaintHub.Infrastructure.Documents.Services;

using System.IO;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

public sealed class ITextDocumentGeneratorService : IDocumentGeneratorService
{
    private readonly IHtmlEngineService _htmlEngineService;

    public ITextDocumentGeneratorService(IHtmlEngineService htmlEngineService)
    {
        _htmlEngineService = htmlEngineService;
    }

    public async Task<byte[]> GenerateTdsAsync(Product product, DocumentTemplate template, TestBatch? testBatch = null, PdfGenerationSettings? settings = null)
    {
        // 1. Coleta os dados e constrói o HTML final do TDS usando o serviço base de HTML
        var html = await _htmlEngineService.GenerateHtmlAsync(product, template, false, testBatch);

        // 2. Renderiza nativamente usando o conversor iText pdfHTML
        return ConvertHtmlToPdf(html);
    }

    public async Task<byte[]> GenerateSdsAsync(Product product, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        // Para SDS com iText (se desejado), a abordagem seria semelhante:
        var html = await _htmlEngineService.GenerateHtmlAsync(product, template, true, null);
        return ConvertHtmlToPdf(html);
    }

    private static byte[] ConvertHtmlToPdf(string htmlString)
    {
        try
        {
            // Remover scripts, pois o iText7 não os executa e o parser pode quebrar com código JS (ex: paged.js)
            var scriptRegex = new System.Text.RegularExpressions.Regex(@"<script[^>]*>[\s\S]*?</script>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            htmlString = scriptRegex.Replace(htmlString, string.Empty);

            // Garantir que a estrutura base HTML existe para o parser do iText
            if (!htmlString.Contains("<html", System.StringComparison.OrdinalIgnoreCase))
            {
                // Se já existir um <head>, injetamos à volta dele, se não, fechamos só num wrapper
                if (htmlString.Contains("<head", System.StringComparison.OrdinalIgnoreCase))
                {
                    htmlString = $"<!DOCTYPE html>\n<html>\n{htmlString}\n</html>";
                }
                else
                {
                    htmlString = $"<!DOCTYPE html>\n<html>\n<body>\n{htmlString}\n</body>\n</html>";
                }
            }

            using var memoryStream = new MemoryStream();
            var converterProperties = new iText.Html2pdf.ConverterProperties();
            iText.Html2pdf.HtmlConverter.ConvertToPdf(htmlString, memoryStream, converterProperties);
            return memoryStream.ToArray();
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"iText Error: {ex.Message} | Inner: {ex.InnerException?.Message}\nStack: {ex.StackTrace}");
        }
    }
}
