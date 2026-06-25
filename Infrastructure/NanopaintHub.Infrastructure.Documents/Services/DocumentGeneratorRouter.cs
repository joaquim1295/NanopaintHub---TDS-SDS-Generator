using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.Documents.DependencyInjection;

namespace NanopaintHub.Infrastructure.Documents.Services;

public class DocumentGeneratorRouter : IDocumentGeneratorService
{
    private readonly IOptionsMonitor<PdfGeneratorOptions> _options;
    private readonly QuestPdfDocumentGeneratorService _questPdfGenerator;
    private readonly DocumentGeneratorService _puppeteerGenerator;
    private readonly ITextDocumentGeneratorService _itextGenerator;

    public DocumentGeneratorRouter(
        IOptionsMonitor<PdfGeneratorOptions> options,
        QuestPdfDocumentGeneratorService questPdfGenerator,
        DocumentGeneratorService puppeteerGenerator,
        ITextDocumentGeneratorService itextGenerator)
    {
        _options = options;
        _questPdfGenerator = questPdfGenerator;
        _puppeteerGenerator = puppeteerGenerator;
        _itextGenerator = itextGenerator;
    }

    public Task<byte[]> GenerateTdsAsync(Product product, DocumentTemplate template, TestBatch? testBatch = null, PdfGenerationSettings? settings = null)
    {
        var engine = ResolveEngine(_options.CurrentValue.TdsEngine);
        return engine.GenerateTdsAsync(product, template, testBatch, settings);
    }

    public Task<byte[]> GenerateSdsAsync(Product product, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        var engine = ResolveEngine(_options.CurrentValue.SdsEngine);
        return engine.GenerateSdsAsync(product, template, settings);
    }

    private IDocumentGeneratorService ResolveEngine(string engineName)
    {
        return engineName?.ToLowerInvariant() switch
        {
            "questpdf" => _questPdfGenerator,
            "itext7" or "itext" => _itextGenerator,
            "webview2" or "html" or _ => _puppeteerGenerator
        };
    }
}
