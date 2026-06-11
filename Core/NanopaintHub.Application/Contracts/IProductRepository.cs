namespace NanopaintHub.Application.Contracts;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByCodeAsync(string code);
    Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm);
    
    Task<IEnumerable<Product>> GetAllAsync();
    
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
}