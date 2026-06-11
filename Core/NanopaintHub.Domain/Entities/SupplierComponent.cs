namespace NanopaintHub.Domain.Entities;

using System;
using NanopaintHub.Domain.Common;

public class SupplierComponent :  Entity
{
    public Guid SupplierId { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string CasNumber { get; set; } = string.Empty; // Identificador Químico Global
    
    // Dados de toxicidade e perigo (Frases H/P, Pictogramas)
    public string SdsProperties { get; set; } = "{}";
    
}