namespace NanopaintHub.Infrastructure.LocalData.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

public class SupplierProductRepository : ISupplierProductRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public SupplierProductRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SupplierProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SupplierProducts
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Compositions)
                .ThenInclude(c => c.EchaSubstance)
                    .ThenInclude(e => e!.Hazards)
                        .ThenInclude(h => h.Phrase)
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);
    }

    public async Task<List<SupplierProduct>> GetAllAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupplierProducts
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Compositions)
                .ThenInclude(c => c.EchaSubstance)
                    .ThenInclude(e => e!.Hazards)
                        .ThenInclude(h => h.Phrase)
            .AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(sp => !sp.IsArchived);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<SupplierProduct>> GetBySupplierIdAsync(Guid supplierId, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupplierProducts
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Compositions)
                .ThenInclude(c => c.EchaSubstance)
                    .ThenInclude(e => e!.Hazards)
                        .ThenInclude(h => h.Phrase)
            .Where(sp => sp.SupplierId == supplierId);

        if (!includeArchived)
        {
            query = query.Where(sp => !sp.IsArchived);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<SupplierProduct>> SearchByNameAsync(string querySearch, int limit = 20, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(querySearch))
            return new List<SupplierProduct>();

        var dbQuery = _dbContext.SupplierProducts
            .Include(sp => sp.Supplier)
            .Include(sp => sp.Compositions)
                .ThenInclude(c => c.EchaSubstance)
                    .ThenInclude(e => e!.Hazards)
                        .ThenInclude(h => h.Phrase)
            .Where(sp => EF.Functions.Like(sp.Name, $"%{querySearch}%"));

        if (!includeArchived)
        {
            dbQuery = dbQuery.Where(sp => !sp.IsArchived);
        }

        return await dbQuery
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SupplierProduct product, CancellationToken cancellationToken = default)
    {
        await _dbContext.SupplierProducts.AddAsync(product, cancellationToken);
    }

    public async Task UpdateAsync(SupplierProduct product, CancellationToken cancellationToken = default)
    {
        product.MarkAsModified();

        // EF Core's Update() handles the graph mapping natively
        // since we are manually syncing the lists in the UseCase and the items are properly tracked.
        _dbContext.SupplierProducts.Update(product);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);
        if (product != null)
        {
            // Verificação de Integridade Referencial (Soft Delete)
            bool isInUse = await _dbContext.ProductCompositions
                .AnyAsync(pc => pc.SupplierProductId == id, cancellationToken);

            if (isInUse)
            {
                throw new InvalidOperationException($"Não é possível excluir '{product.Name}' pois já está a ser utilizado numa fórmula de produto. Considere arquivá-lo.");
            }

            _dbContext.SupplierProducts.Remove(product);
        }
    }
}
