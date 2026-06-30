namespace NanopaintHub.Application.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface IEchaSubstanceRepository
{
    Task<EchaSubstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<EchaSubstance>> SearchByNameAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
    Task<List<EchaSubstance>> SearchByCasAsync(string casNumber, int limit = 20, CancellationToken cancellationToken = default);
    Task<EchaSubstance?> GetByCasAsync(string casNumber, CancellationToken cancellationToken = default);
    Task<List<string>> GetExistingCasNumbersAsync(IEnumerable<string> casNumbers, CancellationToken cancellationToken = default);
    Task AddAsync(EchaSubstance substance, CancellationToken cancellationToken = default);
}
