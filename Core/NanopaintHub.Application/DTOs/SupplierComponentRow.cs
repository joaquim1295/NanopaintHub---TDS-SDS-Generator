namespace NanopaintHub.Application.DTOs;

/// <summary>
/// Linha para composição de SDS (componente químico + fornecedor).
/// </summary>
public sealed record SupplierComponentRow(
    Guid Id,
    string SupplierName,
    string ComponentName,
    string CasNumber,
    string SdsPropertiesJson);
