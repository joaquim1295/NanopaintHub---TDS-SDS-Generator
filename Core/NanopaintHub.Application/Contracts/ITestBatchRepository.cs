using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Contracts;

public interface ITestBatchRepository
{
    Task<TestBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestBatch>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<TestBatch?> GetLatestByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task UpdateAsync(TestBatch batch, CancellationToken cancellationToken = default);
}
