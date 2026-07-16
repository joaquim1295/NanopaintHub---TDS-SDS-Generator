namespace NanopaintHub.Domain.Entities;

using System;
using NanopaintHub.Domain.Common;

/// <summary>
/// Representa a formulação (a "receita") de uma tinta. Liga o produto da Nanopaint às substâncias.
/// </summary>
public class ProductComposition : Entity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? EchaSubstanceId { get; set; }
    public EchaSubstance? EchaSubstance { get; set; }

    public Guid? SupplierProductId { get; set; }
    public SupplierProduct? SupplierProduct { get; set; }

    public double Percentage { get; set; }

    /// <summary>
    /// Campo flexível JSON (Escola A) para propriedades que escapam ao normativo rígido, 
    /// como especificações precisas de tamanho de nanopartículas, etc.
    /// </summary>
    public string ExtraMetadataJson { get; set; } = "{}";

    public string DisplayName => SupplierProduct != null 
        ? SupplierProduct.Name 
        : EchaSubstance?.ChemicalName ?? "Desconhecido";

    public string DisplayDescription => SupplierProduct != null 
        ? $"Matéria-Prima de: {SupplierProduct.Supplier?.Name ?? "Desconhecido"}" 
        : $"CAS: {EchaSubstance?.CasNumber}";
}
