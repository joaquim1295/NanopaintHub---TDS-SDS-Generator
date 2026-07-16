namespace NanopaintHub.Application.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
