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
}