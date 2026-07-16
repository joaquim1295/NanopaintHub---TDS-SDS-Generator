namespace NanopaintHub.Domain.Entities;

using System;
using System.Collections.Generic;
using NanopaintHub.Domain.Common;

public class SupplierProduct : Entity
{
    public string Name { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string SdsFilePath { get; set; } = string.Empty;
    public bool IsArchived { get; set; } = false;

    public ICollection<SupplierProductComposition> Compositions { get; set; } = new List<SupplierProductComposition>();
}
