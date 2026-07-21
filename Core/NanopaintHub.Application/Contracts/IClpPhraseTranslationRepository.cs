namespace NanopaintHub.Application.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

/// <summary>
/// Repositório para leitura das traduções de textos normativos CLP por secção SDS.
/// </summary>
public interface IClpPhraseTranslationRepository
{
    /// <summary>
    /// Retorna os blocos de texto normativos para os códigos H fornecidos, no idioma pedido.
    /// </summary>
    /// <param name="hazardCodes">Lista de códigos H a pesquisar (ex: ["H314", "H318"]).</param>
    /// <param name="languageCode">Código ISO 639-1 (ex: "EN", "PT", "ES").</param>
    Task<IReadOnlyList<ClpPhraseTranslation>> GetTranslationsAsync(
        IEnumerable<string> hazardCodes,
        string languageCode);

    /// <summary>Retorna todos os idiomas disponíveis na base de dados.</summary>
    Task<IReadOnlyList<string>> GetAvailableLanguagesAsync();
}
