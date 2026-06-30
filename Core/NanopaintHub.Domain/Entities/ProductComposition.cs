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

    public Guid EchaSubstanceId { get; set; }
    public EchaSubstance? EchaSubstance { get; set; }

    public double Percentage { get; set; }

    /// <summary>
    /// Campo flexível JSON (Escola A) para propriedades que escapam ao normativo rígido, 
    /// como especificações precisas de tamanho de nanopartículas, etc.
    /// </summary>
    public string ExtraMetadataJson { get; set; } = "{}";
}
