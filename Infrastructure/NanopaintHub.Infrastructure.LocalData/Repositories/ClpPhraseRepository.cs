using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

namespace NanopaintHub.Infrastructure.LocalData.Repositories;

public class ClpPhraseRepository : IClpPhraseRepository
{
    private readonly NanopaintHubDbContext _context;

    public ClpPhraseRepository(NanopaintHubDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClpPhrase>> GetAllAsync()
    {
        return await _context.ClpPhrases.ToListAsync();
    }

    public async Task<ClpPhrase?> GetByCodeAsync(string code)
    {
        return await _context.ClpPhrases.FirstOrDefaultAsync(p => p.Code == code);
    }
}
