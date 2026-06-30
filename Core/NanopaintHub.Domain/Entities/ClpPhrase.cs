namespace NanopaintHub.Domain.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Dicionário oficial de frases de perigo (H) e conselhos de prudência (P) do regulamento CLP.
/// </summary>
public class ClpPhrase
{
    [Key]
    public string Code { get; set; } = string.Empty; // ex: H225
    public string Description { get; set; } = string.Empty; // ex: Líquido e vapor facilmente inflamáveis.
    
    // Indica se é 'H' (Hazard/Perigo) ou 'P' (Precautionary/Prudência)
    public string PhraseType { get; set; } = "H"; 
}
