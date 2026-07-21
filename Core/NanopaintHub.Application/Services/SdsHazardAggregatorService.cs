namespace NanopaintHub.Application.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

/// <summary>
/// Motor de Regras de Segurança Dirigido por Perigos (Hazard-Driven Rules Engine).
/// 
/// Percorre a composição completa do produto (incluindo Matérias-Primas de fornecedores),
/// agrega os perigos ECHA e resolve os textos normativos obrigatórios por secção SDS.
/// 
/// Cada resultado carrega a sua origem rastreável (Qual H-Phrase + Qual Matéria-Prima),
/// permitindo ao Químico verificar porque é que cada instrução de segurança foi gerada.
/// </summary>
public class SdsHazardAggregatorService : ISdsHazardAggregatorService
{
    private readonly IClpPhraseTranslationRepository _translationRepo;

    public SdsHazardAggregatorService(IClpPhraseTranslationRepository translationRepo)
    {
        _translationRepo = translationRepo;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SdsHazardRecommendation>> AggregateAsync(Product product, string languageCode = "EN")
    {
        // 1. Recolher todos os (Hazard, MaterialName) da árvore de composição
        var hazardSources = CollectHazardSources(product);

        if (!hazardSources.Any())
            return Array.Empty<SdsHazardRecommendation>();

        // 2. Obter as traduções para os códigos H identificados
        var distinctCodes = hazardSources.Select(hs => hs.HazardCode).Distinct().ToList();
        var translations = await _translationRepo.GetTranslationsAsync(distinctCodes, languageCode);

        // 3. Montar os resultados com rastreabilidade
        var recommendations = new List<SdsHazardRecommendation>();

        foreach (var translation in translations)
        {
            // Encontrar a primeira fonte que ativou este perigo (para rastreabilidade na UI)
            var source = hazardSources
                .First(hs => hs.HazardCode == translation.ClpPhraseCode);

            recommendations.Add(new SdsHazardRecommendation
            {
                SectionType    = translation.SectionType,
                Text           = translation.TranslatedText,
                SourceHazardCode  = translation.ClpPhraseCode,
                SourceMaterialName = source.MaterialName
            });
        }

        // Ordenar por secção para renderização sequencial no PDF
        return recommendations
            .OrderBy(r => r.SectionType)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc/>
    public string ComputeHazardHash(Product product)
    {
        var allCodes = product.Compositions
            .Where(c => c.EchaSubstance?.Hazards != null)
            .SelectMany(c => c.EchaSubstance!.Hazards!.Select(h => h.PhraseCode ?? string.Empty))
            .Where(code => !string.IsNullOrWhiteSpace(code) && code.StartsWith("H"))
            .Distinct()
            .OrderBy(c => c);

        return string.Join(";", allCodes);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────────────────────────────────────

    private record HazardSource(string HazardCode, string MaterialName);

    /// <summary>
    /// Percorre a composição de 2 níveis (produto → matéria-prima directa | produto → MP de fornecedor → substância).
    /// Retorna todos os pares (código H, nome da matéria-prima) distintos.
    /// </summary>
    private List<HazardSource> CollectHazardSources(Product product)
    {
        var sources = new List<HazardSource>();

        foreach (var comp in product.Compositions ?? Enumerable.Empty<ProductComposition>())
        {
            // Nível 1: Substância ECHA directa
            if (comp.EchaSubstance?.Hazards != null)
            {
                var materialName = comp.EchaSubstance.ChemicalName ?? "Substância Desconhecida";
                foreach (var hazard in comp.EchaSubstance.Hazards)
                {
                    if (!string.IsNullOrWhiteSpace(hazard.PhraseCode) && hazard.PhraseCode.StartsWith("H"))
                        sources.Add(new HazardSource(hazard.PhraseCode, materialName));
                }
            }

            // Nível 2: Composição de um produto de fornecedor (BOM flattenning)
            if (comp.SupplierProduct?.Compositions != null)
            {
                foreach (var innerComp in comp.SupplierProduct.Compositions)
                {
                    if (innerComp.EchaSubstance?.Hazards == null) continue;
                    var materialName = $"{comp.SupplierProduct.Name} → {innerComp.EchaSubstance.ChemicalName}";
                    foreach (var hazard in innerComp.EchaSubstance.Hazards)
                    {
                        if (!string.IsNullOrWhiteSpace(hazard.PhraseCode) && hazard.PhraseCode.StartsWith("H"))
                            sources.Add(new HazardSource(hazard.PhraseCode, materialName));
                    }
                }
            }
        }

        // Deduplica por código (mantém a 1ª fonte para rastreabilidade)
        return sources
            .GroupBy(s => s.HazardCode)
            .Select(g => g.First())
            .ToList();
    }
}
