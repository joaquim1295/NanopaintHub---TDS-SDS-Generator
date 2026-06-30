namespace NanopaintHub.Infrastructure.LocalData.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

public class EchaSubstanceRepository : IEchaSubstanceRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public EchaSubstanceRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EchaSubstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EchaSubstances
            .Include(e => e.Hazards)
            .ThenInclude(h => h.Phrase)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<EchaSubstance>> SearchByNameAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<EchaSubstance>();

        return await _dbContext.EchaSubstances
            .Include(e => e.Hazards)
            .ThenInclude(h => h.Phrase)
            .Where(e => EF.Functions.Like(e.ChemicalName, $"%{query}%"))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EchaSubstance>> SearchByCasAsync(string casNumber, int limit = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(casNumber))
            return new List<EchaSubstance>();

        return await _dbContext.EchaSubstances
            .Include(e => e.Hazards)
            .ThenInclude(h => h.Phrase)
            .Where(e => e.CasNumber.Contains(casNumber))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<EchaSubstance?> GetByCasAsync(string casNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(casNumber)) return null;

        return await _dbContext.EchaSubstances
            .Include(e => e.Hazards)
            .ThenInclude(h => h.Phrase)
            .FirstOrDefaultAsync(e => e.CasNumber == casNumber, cancellationToken);
    }

    public async Task AddAsync(EchaSubstance substance, CancellationToken cancellationToken = default)
    {
        await _dbContext.EchaSubstances.AddAsync(substance, cancellationToken);
    }

    public async Task<List<string>> GetExistingCasNumbersAsync(IEnumerable<string> casNumbers, CancellationToken cancellationToken = default)
    {
        if (casNumbers == null || !casNumbers.Any()) return new List<string>();

        return await _dbContext.EchaSubstances
            .Where(e => casNumbers.Contains(e.CasNumber))
            .Select(e => e.CasNumber)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
