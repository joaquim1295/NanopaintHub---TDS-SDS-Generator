namespace NanopaintHub.Infrastructure.LocalData.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

/// <summary>
/// Escrita sem <c>SaveChanges</c> automático — após Add/Update/Delete, chame <see cref="IUnitOfWork.CommitAsync"/> no mesmo âmbito.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public ProductRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByCodeAsync(string code)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm)
    {
        return await _dbContext.Products
            .Where(p => EF.Functions.Like(p.Name.ToLower(), $"%{searchTerm.ToLower()}%"))
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbContext.Products.ToListAsync();
    }
    

    public async Task AddAsync(Product product)
    {

        await _dbContext.Products.AddAsync(product);
    }

    public Task UpdateAsync(Product product)
    {
        product.MarkAsModified(); 
        _dbContext.Products.Update(product); 
        return Task.CompletedTask; 
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await GetByIdAsync(id);
        if (product != null)
        {
            _dbContext.Products.Remove(product); 
        }
    }
}