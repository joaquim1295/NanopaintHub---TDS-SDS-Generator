namespace Microsoft.Extensions.DependencyInjection;

using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Infrastructure.Documents.Services;
using NanopaintHub.Infrastructure.Documents.Tds;

using Microsoft.Extensions.Configuration;
using NanopaintHub.Infrastructure.Documents.DependencyInjection;

public static class NanopaintHubDocumentsServiceCollectionExtensions
{
    public static IServiceCollection AddNanopaintHubDocuments(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PdfGeneratorOptions>(configuration.GetSection("PdfGenerator"));
        services.AddSingleton<IPdfGenerationProgressNotifier, PdfGenerationProgressNotifier>();
        services.AddSingleton<IHtmlPreviewService, HtmlPreviewService>();
        services.AddSingleton<IChromiumTinyMceEditorService, ChromiumTinyMceEditorService>();
        services.AddSingleton<TdsDataMergeService>();
        services.AddSingleton<ITdsChartRenderer, TdsChartSvgBuilder>();
        services.AddSingleton<ITdsHtmlGenerator, TdsHtmlGenerator>();
        services.AddScoped<IPdfLayoutService, TdsPdfLayoutService>();
        services.AddScoped<TdsDataGatherer>();
        services.AddScoped<SdsDataGatherer>();
        services.AddScoped<DocumentGeneratorService>();
        services.AddScoped<QuestPdfDocumentGeneratorService>();
        services.AddScoped<ITextDocumentGeneratorService>();
        services.AddScoped<ISdsPdfExtractorService, SdsPdfExtractorService>();
        services.AddScoped<IDocumentGeneratorService, DocumentGeneratorRouter>();
        services.AddScoped<IHtmlEngineService>(sp => sp.GetRequiredService<DocumentGeneratorService>());
        return services;
    }
}
