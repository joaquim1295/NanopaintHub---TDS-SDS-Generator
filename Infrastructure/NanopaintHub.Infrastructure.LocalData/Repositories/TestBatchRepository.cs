using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

namespace NanopaintHub.Infrastructure.LocalData.Repositories;

public sealed class TestBatchRepository : ITestBatchRepository
{
    private readonly NanopaintHubDbContext _db;

    public TestBatchRepository(NanopaintHubDbContext db) => _db = db;

    public Task<TestBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.TestBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TestBatch>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default) =>
        await _db.TestBatches
            .Where(b => b.ProductId == productId)
            .OrderByDescending(b => b.ManufactureDate)
            .ThenByDescending(b => b.BatchNumber)
            .ToListAsync(cancellationToken);

    public Task<TestBatch?> GetLatestByProductIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        _db.TestBatches
            .Where(b => b.ProductId == productId)
            .OrderByDescending(b => b.ManufactureDate)
            .ThenByDescending(b => b.BatchNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public Task UpdateAsync(TestBatch batch, CancellationToken cancellationToken = default)
    {
        batch.MarkAsModified();
        _db.TestBatches.Update(batch);
        return Task.CompletedTask;
    }
}
