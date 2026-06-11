using NanopaintHub.Application.DTOs;

namespace NanopaintHub.Application.Contracts;

public interface ISupplierComponentRepository
{
    Task<IReadOnlyList<SupplierComponentRow>> GetAllWithSupplierAsync(CancellationToken cancellationToken = default);
}
