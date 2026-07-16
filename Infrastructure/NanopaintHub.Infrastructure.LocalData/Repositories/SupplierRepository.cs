namespace NanopaintHub.Infrastructure.LocalData.Repositories;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

public class SupplierRepository : ISupplierRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public SupplierRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .Include(s => s.Products)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await _dbContext.Suppliers.AddAsync(supplier, cancellationToken);
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _dbContext.Suppliers.Update(supplier);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await GetByIdAsync(id, cancellationToken);
        if (supplier != null)
        {
            _dbContext.Suppliers.Remove(supplier);
        }
    }
}
