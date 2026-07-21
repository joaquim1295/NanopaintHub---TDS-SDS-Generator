namespace NanopaintHub.Domain.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Tabela de tradução de textos normativos associados a frases de perigo CLP.
/// Satisfaz o requisito REACH Artigo 31 de emissão de SDS no idioma oficial do país de comercialização.
/// 
/// Chave primária composta: (ClpPhraseCode, LanguageCode, SectionType)
/// </summary>
public class ClpPhraseTranslation
{
    /// <summary>Código da frase CLP de origem. Ex: "H314", "H318".</summary>
    [Required, MaxLength(20)]
    public string ClpPhraseCode { get; set; } = string.Empty;

    /// <summary>Código ISO 639-1 do idioma. Ex: "EN", "PT", "ES", "FR".</summary>
    [Required, MaxLength(5)]
    public string LanguageCode { get; set; } = "EN";

    /// <summary>
    /// Identificador da secção da SDS onde este texto é aplicado.
    /// Valores normalizados: "Sec4_FirstAid" | "Sec5_Firefighting" | "Sec6_AccidentalRelease" 
    ///                      | "Sec7_Handling" | "Sec8_Exposure" | "Sec13_Disposal"
    /// </summary>
    [Required, MaxLength(50)]
    public string SectionType { get; set; } = string.Empty;

    /// <summary>
    /// Texto normativo traduzido, para inserção direta no PDF.
    /// Gerado com base nos regulamentos CLP/REACH e validado por um Químico Responsável.
    /// </summary>
    [Required]
    public string TranslatedText { get; set; } = string.Empty;

    // Navegação para a frase CLP
    [ForeignKey(nameof(ClpPhraseCode))]
    public ClpPhrase? Phrase { get; set; }
}
