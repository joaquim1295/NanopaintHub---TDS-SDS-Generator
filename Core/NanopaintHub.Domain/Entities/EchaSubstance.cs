namespace NanopaintHub.Domain.Entities;

using NanopaintHub.Domain.Common;

/// <summary>
/// Representa uma substância química oficial (Ex: Anexo VI da ECHA).
/// </summary>
public class EchaSubstance : Entity
{
    public string ChemicalName { get; set; } = string.Empty;
    public string CasNumber { get; set; } = string.Empty;
    public string EcNumber { get; set; } = string.Empty;

    // Relação N:M com as frases de perigo (H) e prudência (P)
    public ICollection<SubstanceHazard> Hazards { get; set; } = new List<SubstanceHazard>();
}
