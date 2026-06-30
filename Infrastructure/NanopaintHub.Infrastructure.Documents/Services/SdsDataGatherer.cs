namespace NanopaintHub.Infrastructure.Documents.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HandlebarsDotNet;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Application.Tds;
using NanopaintHub.Infrastructure.Documents.Tds;

public class SdsDataGatherer
{
    public SdsDataGatherer()
    {
    }

    public Task<string> GatherHtmlAsync(Product product, DocumentTemplate template)
    {
        var featuresList = SafeDeserialize(product.FeaturesJson, new List<string>(), "FeaturesJson");
        var liquidProps = SafeDeserialize(product.LiquidPropertiesJson, new Dictionary<string, string>(), "LiquidPropertiesJson");
        var mechanicalProps = SafeDeserialize(product.MechanicalPropertiesJson, new Dictionary<string, string>(), "MechanicalPropertiesJson");
        var functionalProps = SafeDeserialize(product.FunctionalPropertiesJson, new Dictionary<string, string>(), "FunctionalPropertiesJson");
        var jsonExtendedSpecs = TdsSensorDataMerger.ParseProductSpecs(product.TdsSensorSpecsJson);

        var components = product.Compositions?
            .Select(c => (object)new
            {
                ChemicalName = c.EchaSubstance?.ChemicalName,
                CasNumber = c.EchaSubstance?.CasNumber,
                EcNumber = c.EchaSubstance?.EcNumber,
                Percentage = $"{c.Percentage}%"
            })
            .ToList() ?? new List<object>();

        var templateData = new
        {
            product.Name,
            product.Code,
            product.Description,
            RevisionDate = product.RevisionDate.ToString("dd/MM/yyyy"),
            Version = product.DocumentVersion,
            Features = featuresList,
            LiquidProperties = liquidProps,
            MechanicalProperties = mechanicalProps,
            FunctionalProperties = functionalProps,
            Components = components,
            DocumentKind = "SDS",
            Placeholders = jsonExtendedSpecs.PlaceholderValues,
            Assets = jsonExtendedSpecs.TemplateAssets
        };

        var compiledTemplate = Handlebars.Compile(template.HtmlStructure);
        var innerHtml = compiledTemplate(templateData);

        if (string.IsNullOrWhiteSpace(innerHtml))
        {
            throw new InvalidOperationException($"Handlebars render devolveu HTML vazio para SDS.");
        }

        return Task.FromResult(innerHtml);
    }

    public Task<object> GatherDataRawAsync(Product product)
    {
        var components = product.Compositions?
            .Select(c => (object)new
            {
                ChemicalName = c.EchaSubstance?.ChemicalName,
                CasNumber = c.EchaSubstance?.CasNumber,
                EcNumber = c.EchaSubstance?.EcNumber,
                Percentage = $"{c.Percentage}%"
            })
            .ToList() ?? new List<object>();

        return Task.FromResult<object>(new { Product = product, Components = components });
    }

    private static T SafeDeserialize<T>(string? json, T fallback, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;
        try { return JsonSerializer.Deserialize<T>(json) ?? fallback; }
        catch (JsonException ex) { throw new InvalidOperationException($"JSON inválido no campo {fieldName}: {ex.Message}", ex); }
    }
}
