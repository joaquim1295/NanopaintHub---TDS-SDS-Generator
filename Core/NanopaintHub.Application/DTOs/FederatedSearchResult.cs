namespace NanopaintHub.Application.DTOs;

using System;
using NanopaintHub.Domain.Entities;

public class FederatedSearchResult
{
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Exactly one of these will be non-null, unless it's a flattened origin
    public EchaSubstance? EchaSubstance { get; set; }
    public SupplierProduct? SupplierProduct { get; set; }
    public SupplierProduct? OriginSupplierProduct { get; set; }

    public bool IsImportAction { get; set; } = false;
    public string TypedQuery { get; set; } = string.Empty;

    public bool IsRawMaterial => SupplierProduct != null;
}
