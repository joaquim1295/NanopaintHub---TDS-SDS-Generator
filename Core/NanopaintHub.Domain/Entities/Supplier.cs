namespace NanopaintHub.Domain.Entities;

using NanopaintHub.Domain.Common;

public class Supplier : Entity 
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string TechnicalContact { get; set; } = string.Empty;

    public ICollection<SupplierProduct> Products { get; set; } = new List<SupplierProduct>();
}