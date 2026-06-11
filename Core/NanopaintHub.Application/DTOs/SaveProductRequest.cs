namespace NanopaintHub.Application.DTOs;

/// <summary>Dados para criar ou actualizar um produto (JSONs como texto validado na aplicação).</summary>
public sealed record SaveProductRequest(
    Guid? Id,
    string Code,
    string Name,
    string Description,
    Guid? CategoryId,
    string FeaturesJson,
    string LiquidPropertiesJson,
    string MechanicalPropertiesJson,
    string FunctionalPropertiesJson,
    string TdsSensorSpecsJson,
    DateTime RevisionDate,
    int DocumentVersion);
