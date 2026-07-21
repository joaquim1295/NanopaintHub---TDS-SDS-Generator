namespace NanopaintHub.Domain.Entities;

using NanopaintHub.Domain.Common;

public class Product : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public Guid CategoryId { get; set; }
    
    public string FeaturesJson { get; set; } = "[]";
    // Os 3 grupos de caracterização industrial da Nanopaint
    public string LiquidPropertiesJson { get; set; } = "{}";       
    public string MechanicalPropertiesJson { get; set; } = "{}";   
    public string FunctionalPropertiesJson { get; set; } = "{}";   

    /// <summary>Especificações TDS de sensor.</summary>
    public string TdsSensorSpecsJson { get; set; } = "{}";

    public DateTime RevisionDate { get; set; }
    public int DocumentVersion { get; set; }
    public string ExposureScenarioPdfPath { get; set; } = string.Empty;

    // SDS Configuration (CLP)
    public string PrecautionaryCodesJson { get; set; } = "[]";
    public string SdsSection4CustomText { get; set; } = string.Empty;
    public string SdsSection7CustomText { get; set; } = string.Empty;

    /// <summary>
    /// JSON serializado de <see cref="SdsOverrideData"/>.
    /// Guarda os textos manuais do Químico por secção (ex: Sec4_FirstAid, Sec14_Transport).
    /// Quando null ou vazio, o sistema usa a geração automática por perigos ECHA.
    /// </summary>
    public string SdsOverrideJson { get; set; } = string.Empty;

    /// <summary>
    /// Impressão digital dos perigos activos no momento em que os Overrides foram guardados.
    /// Formato: frases H concatenadas e ordenadas (ex: "H225;H314;H318").
    /// Usado para detetar se a fórmula mudou de perfil de perigo desde o último override manual
    /// (Staleness Detection - Proteção contra overrides desatualizados).
    /// </summary>
    public string HazardProfileHash { get; set; } = string.Empty;

    public ICollection<ProductComposition> Compositions { get; set; } = new List<ProductComposition>();
}
