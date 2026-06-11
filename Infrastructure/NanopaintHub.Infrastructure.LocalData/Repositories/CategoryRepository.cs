using System.Linq;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

namespace NanopaintHub.Infrastructure.LocalData.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public CategoryRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
