namespace NanopaintHub.Infrastructure.LocalData.Persistence;

using NanopaintHub.Application.Contracts;
using System.Threading.Tasks;

public class UnitOfWork : IUnitOfWork
{
    private readonly NanopaintHubDbContext _dbContext;

    public UnitOfWork(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CommitAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}