namespace Microsoft.Extensions.DependencyInjection;

using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Infrastructure.Documents.Services;
using NanopaintHub.Infrastructure.Documents.Tds;

public static class NanopaintHubDocumentsServiceCollectionExtensions
{
    public static IServiceCollection AddNanopaintHubDocuments(this IServiceCollection services)
    {
        services.AddSingleton<IPdfGenerationProgressNotifier, PdfGenerationProgressNotifier>();
        services.AddSingleton<IHtmlPreviewService, HtmlPreviewService>();
        services.AddSingleton<IChromiumTinyMceEditorService, ChromiumTinyMceEditorService>();
        services.AddSingleton<TdsDataMergeService>();
        services.AddSingleton<ITdsChartRenderer, TdsChartSvgBuilder>();
        services.AddScoped<IDocumentGeneratorService, DocumentGeneratorService>();
        return services;
    }
}
