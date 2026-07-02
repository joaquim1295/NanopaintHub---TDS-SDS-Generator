namespace NanopaintHub.Infrastructure.Documents.Services;

using System;
using System.Collections.Generic;
using System.IO;
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

    public async Task<string> GatherHtmlAsync(Product product, DocumentTemplate template)
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
                Percentage = $"{c.Percentage}%",
                Hazards = string.Join("; ", c.EchaSubstance?.Hazards?.Select(h => h.PhraseCode) ?? Array.Empty<string>())
            })
            .ToList() ?? new List<object>();

        // 1. Extrair todas as Unique Hazards
        var compositions = product.Compositions?.ToList() ?? new List<ProductComposition>();
        var uniqueHazards = compositions
            .Where(c => c.EchaSubstance?.Hazards != null)
            .SelectMany(c => c.EchaSubstance!.Hazards!)
            .GroupBy(h => h.PhraseCode)
            .Select(g => g.First())
            .ToList();
            
        var hazardPhrases = uniqueHazards
            .Select(h => $"{h.PhraseCode}: {(h.Phrase?.Description ?? "Hazard description not available")}")
            .ToList();
            
        var hazardCodes = uniqueHazards.Select(h => h.PhraseCode ?? "").ToHashSet();

        // 2. Determinar Pictogramas aplicáveis
        var pictograms = new List<string>();
        if (hazardCodes.Any(h => h.StartsWith("H315") || h.StartsWith("H319") || h.StartsWith("H335"))) pictograms.Add("GHS07 (Exclamation mark)");
        if (hazardCodes.Any(h => h.StartsWith("H318") || h.StartsWith("H314"))) pictograms.Add("GHS05 (Corrosion)");
        if (hazardCodes.Any(h => h.StartsWith("H225") || h.StartsWith("H226"))) pictograms.Add("GHS02 (Flame)");
        if (hazardCodes.Any(h => h.StartsWith("H410") || h.StartsWith("H411"))) pictograms.Add("GHS09 (Environment)");
        if (hazardCodes.Any(h => h.StartsWith("H301") || h.StartsWith("H311") || h.StartsWith("H331"))) pictograms.Add("GHS06 (Skull and crossbones)");
        if (hazardCodes.Any(h => h.StartsWith("H350") || h.StartsWith("H340") || h.StartsWith("H360"))) pictograms.Add("GHS08 (Health hazard)");

        // 3. Flags Lógicas para o Handlebars
        bool isFlammable = hazardCodes.Any(h => h.StartsWith("H22"));
        bool hasInhalationHazard = hazardCodes.Any(h => h.StartsWith("H33"));
        bool hasSkinHazard = hazardCodes.Any(h => h.StartsWith("H31"));
        bool hasEyeHazard = hazardCodes.Any(h => h == "H319" || h == "H318");
        bool hasIngestionHazard = hazardCodes.Any(h => h.StartsWith("H30"));
        bool isToxic = hazardCodes.Any(h => h.StartsWith("H30") || h.StartsWith("H31") || h.StartsWith("H33"));
        bool isEcoToxic = hazardCodes.Any(h => h.StartsWith("H4"));

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var schema = string.IsNullOrWhiteSpace(template.BuilderJson) 
            ? new TemplateBuilderSchema() 
            : JsonSerializer.Deserialize<TemplateBuilderSchema>(template.BuilderJson, options) ?? new TemplateBuilderSchema();

        string logoBase64 = string.Empty;
        if (schema.Header.ShowLogo)
        {
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Nanopaint_logo.png");
            if (File.Exists(logoPath))
            {
                var bytes = await File.ReadAllBytesAsync(logoPath);
                logoBase64 = Convert.ToBase64String(bytes);
            }
        }

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
            Assets = jsonExtendedSpecs.TemplateAssets,
            Schema = schema,
            LogoBase64 = logoBase64,
            UniqueHazards = uniqueHazards,
            HasHazards = uniqueHazards.Any(),
            Pictograms = pictograms,
            HasPictograms = pictograms.Any(),
            HasInhalationHazard = hasInhalationHazard,
            HasSkinHazard = hasSkinHazard,
            HasEyeHazard = hasEyeHazard,
            HasIngestionHazard = hasIngestionHazard,
            IsToxic = isToxic,
            IsEcoToxic = isEcoToxic,
            HazardPhrases = hazardPhrases,
            HasHazardPhrases = hazardPhrases.Any()
        };

        var compiledTemplate = Handlebars.Compile(template.HtmlStructure);
        var innerHtml = compiledTemplate(templateData);

        if (string.IsNullOrWhiteSpace(innerHtml))
        {
            throw new InvalidOperationException($"Handlebars render devolveu HTML vazio para SDS.");
        }

        return innerHtml;
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
