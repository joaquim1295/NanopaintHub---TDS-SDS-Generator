namespace NanopaintHub.Application.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface ISupplierProductRepository
{
    Task<SupplierProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<SupplierProduct>> GetAllAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<List<SupplierProduct>> GetBySupplierIdAsync(Guid supplierId, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<List<SupplierProduct>> SearchByNameAsync(string query, int limit = 20, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task AddAsync(SupplierProduct product, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupplierProduct product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
