using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Contracts;

public interface ICategoryRepository
{
    Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default);
}
