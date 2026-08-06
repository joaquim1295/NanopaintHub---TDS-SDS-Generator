namespace NanopaintHub.Infrastructure.Documents.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HandlebarsDotNet;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.Documents.Tds;

public class TdsDataGatherer
{
    private readonly TdsDataMergeService _tdsMerge;
    private readonly ITdsChartRenderer _chartRenderer;
    private readonly IPdfLayoutService _pdfLayout;

    public TdsDataGatherer(
        TdsDataMergeService tdsMerge,
        ITdsChartRenderer chartRenderer,
        IPdfLayoutService pdfLayout)
    {
        _tdsMerge = tdsMerge;
        _chartRenderer = chartRenderer;
        _pdfLayout = pdfLayout;
    }

    public Task<string> GatherHtmlAsync(Product product, DocumentTemplate template, TestBatch? testBatch = null)
    {
        var featuresList = SafeDeserialize(product.FeaturesJson, new List<string>(), "FeaturesJson");
        var liquidProps = SafeDeserialize(product.LiquidPropertiesJson, new Dictionary<string, string>(), "LiquidPropertiesJson");
        var mechanicalProps = SafeDeserialize(product.MechanicalPropertiesJson, new Dictionary<string, string>(), "MechanicalPropertiesJson");
        var functionalProps = SafeDeserialize(product.FunctionalPropertiesJson, new Dictionary<string, string>(), "FunctionalPropertiesJson");

        var mergedSpecs = _tdsMerge.Merge(product, testBatch);
        var imageFallback = testBatch?.CurveGraphPath;
        var chartSvgs = _chartRenderer.RenderAll(
            mergedSpecs,
            linearityImagePath: imageFallback,
            resistanceImagePath: imageFallback,
            hysteresisImagePath: imageFallback);
        var batchNumber = _tdsMerge.ResolveBatchNumber(testBatch);

        var logoBase64 = ReadLogoBase64();
        var productImageBase64 = ReadImageBase64(product.ProductImagePath);
        var templateData = TdsTemplateViewModelBuilder.Build(
            product.Name,
            product.Code,
            product.ProductCategory?.Name ?? string.Empty,
            product.ProductType ?? string.Empty,
            product.Description,
            product.RevisionDate.ToString("dd/MM/yyyy"),
            product.DocumentVersion,
            featuresList,
            liquidProps,
            mechanicalProps,
            functionalProps,
            mergedSpecs,
            chartSvgs,
            batchNumber,
            logoBase64,
            productImageBase64);

        TemplateBuilderSchema? schema = null;
        if (!string.IsNullOrWhiteSpace(template.BuilderJson))
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            schema = JsonSerializer.Deserialize<TemplateBuilderSchema>(template.BuilderJson, options);
        }

        if (schema == null || schema.Sections == null || schema.Sections.Count == 0)
        {
            schema = TemplateHtmlParser.ParseToSchema(template.HtmlStructure, template.CssStructure);
        }

        _pdfLayout.NormalizeLayout(schema, template);

        var innerHtml = AdvancedDocumentRenderer.Render(schema!, templateData);

        if (string.IsNullOrWhiteSpace(innerHtml))
        {
            throw new InvalidOperationException($"Handlebars render devolveu HTML vazio para TDS.");
        }

        return Task.FromResult(innerHtml);
    }

    private static T SafeDeserialize<T>(string? json, T fallback, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;
        try { return JsonSerializer.Deserialize<T>(json) ?? fallback; }
        catch (JsonException ex) { throw new InvalidOperationException($"JSON inválido no campo {fieldName}: {ex.Message}", ex); }
    }

    private static string? ReadLogoBase64()
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Nanopaint_logo.png");
        if (!File.Exists(logoPath)) return null;
        var bytes = File.ReadAllBytes(logoPath);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private static string? ReadImageBase64(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var fallbackPath = Path.Combine(AppContext.BaseDirectory, "Assets", "GenericProductPhoto.png");
            if (File.Exists(fallbackPath))
            {
                path = fallbackPath;
            }
            else return null;
        }
        try
        {
            var bytes = File.ReadAllBytes(path);
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var mime = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "image/png"
            };
            return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        }
        catch { return null; }
    }
}
