namespace NanopaintHub.Application.Contracts;

using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface IDocumentGeneratorService
{
    // Emissão de TDS (Ficha Técnica) - Consome metadados e especificações técnicas do produto
    Task<byte[]> GenerateTdsAsync(Product product, DocumentTemplate template, TestBatch? testBatch = null, PdfGenerationSettings? settings = null);

    // Emissão de SDS (Ficha de Segurança) - Consome perigos e propriedades químicas do produto
    Task<byte[]> GenerateSdsAsync(Product product, DocumentTemplate template, PdfGenerationSettings? settings = null);

    // Métodos para permitir visualização e edição intermediária
    Task<string> GenerateHtmlAsync(Product product, DocumentTemplate template, bool isSds, TestBatch? testBatch = null);
    Task<byte[]> GeneratePdfFromHtmlAsync(string html, DocumentTemplate template, PdfGenerationSettings? settings = null);
}