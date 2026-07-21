namespace NanopaintHub.Domain.Entities;

using System.Collections.Generic;

/// <summary>
/// DTO de Override Manual da SDS guardado em JSON na tabela Products.
/// Permite que o Químico sobreponha os textos gerados automaticamente.
/// O campo HazardProfileHash do Produto deteta quando esta estrutura fica desatualizada.
/// </summary>
public class SdsOverrideData
{
    /// <summary>Hash dos perigos ativos quando os Overrides foram guardados pela última vez.</summary>
    public string? HazardProfileHash { get; set; }

    /// <summary>Data do último override manual guardado.</summary>
    public DateTime? LastOverrideSavedAt { get; set; }

    /// <summary>
    /// Dicionário de overrides por secção.
    /// Chave: SectionType normalizado (ex: "Sec4_FirstAid", "Sec14_Transport").
    /// Valor: Texto livre validado pelo Químico Responsável.
    /// </summary>
    public Dictionary<string, string> SectionOverrides { get; set; } = new();
}
