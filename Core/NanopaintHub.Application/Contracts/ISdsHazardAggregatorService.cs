namespace NanopaintHub.Application.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Resultado de uma recomendação de segurança gerada automaticamente pelo motor SDS.
/// Transporta o texto normativo E a sua origem rastreável (qual perigo, qual matéria-prima).
/// Satisfaz o requisito de Rastreabilidade: "De onde veio esta instrução?"
/// </summary>
public record SdsHazardRecommendation
{
    /// <summary>Secção normalizada onde este texto é exibido. Ex: "Sec4_FirstAid".</summary>
    public string SectionType { get; init; } = string.Empty;

    /// <summary>Texto normativo a imprimir na SDS (no idioma solicitado).</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Código da frase H que activou esta recomendação. Ex: "H318".</summary>
    public string SourceHazardCode { get; init; } = string.Empty;

    /// <summary>Nome da substância ou Matéria-Prima que introduziu este perigo na fórmula.</summary>
    public string SourceMaterialName { get; init; } = string.Empty;
}

/// <summary>
/// Contrato para o motor de agregação de recomendações de segurança.
/// Implementação em NanopaintHub.Application.Services.SdsHazardAggregatorService.
/// </summary>
public interface ISdsHazardAggregatorService
{
    /// <summary>
    /// Agrega os perigos do produto e gera blocos de texto normativos rastreáveis.
    /// </summary>
    /// <param name="product">Produto com a sua composição carregada (include Compositions.EchaSubstance.Hazards).</param>
    /// <param name="languageCode">Código ISO 639-1. Ex: "EN", "PT", "ES".</param>
    /// <returns>Lista de recomendações, cada uma ligada à sua H-Phrase e matéria-prima de origem.</returns>
    Task<IReadOnlyList<SdsHazardRecommendation>> AggregateAsync(
        NanopaintHub.Domain.Entities.Product product,
        string languageCode = "EN");

    /// <summary>
    /// Calcula o hash do perfil de perigos actuais do produto.
    /// Formato: frases H ordenadas e concatenadas com ";". Ex: "H225;H314;H318".
    /// </summary>
    string ComputeHazardHash(NanopaintHub.Domain.Entities.Product product);
}
