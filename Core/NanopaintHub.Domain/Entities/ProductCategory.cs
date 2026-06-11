using NanopaintHub.Domain.Common;

namespace NanopaintHub.Domain.Entities;

public class ProductCategory : Entity
{
    public string Name { get; set; } = string.Empty;       
    public string Description { get; set; } = string.Empty; 
}