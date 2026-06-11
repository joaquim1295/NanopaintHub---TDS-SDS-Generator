namespace NanopaintHub.Infrastructure.LocalData.Repositories;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.DTOs;
using NanopaintHub.Infrastructure.LocalData.Persistence;

public sealed class SupplierComponentRepository : ISupplierComponentRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public SupplierComponentRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SupplierComponentRow>> GetAllWithSupplierAsync(
        CancellationToken cancellationToken = default)
    {
        var query =
            from sc in _dbContext.SupplierComponents
            join s in _dbContext.Suppliers on sc.SupplierId equals s.Id
            orderby s.Name, sc.ComponentName
            select new SupplierComponentRow(sc.Id, s.Name, sc.ComponentName, sc.CasNumber, sc.SdsProperties);

        return await query.ToListAsync(cancellationToken);
    }
}
