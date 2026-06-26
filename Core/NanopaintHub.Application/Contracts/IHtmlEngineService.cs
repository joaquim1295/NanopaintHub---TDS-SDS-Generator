namespace NanopaintHub.Application.Contracts;

using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface IHtmlEngineService
{
    // Métodos isolados para conversão e preview de HTML, usados apenas internamente ou pelos editores (TDS).
    Task<string> GenerateRawBodyHtmlAsync(Product product, DocumentTemplate template, bool isSds = false, TestBatch? testBatch = null);
    Task<string> GenerateHtmlAsync(Product product, DocumentTemplate template, bool asEditorPreview = false, TestBatch? testBatch = null);
    Task<byte[]> GeneratePdfFromHtmlAsync(string html, DocumentTemplate template, PdfGenerationSettings? settings = null);
    Task<byte[]> GeneratePdfFromRawBodyAsync(string bodyHtml, DocumentTemplate template, PdfGenerationSettings? settings = null);
}
