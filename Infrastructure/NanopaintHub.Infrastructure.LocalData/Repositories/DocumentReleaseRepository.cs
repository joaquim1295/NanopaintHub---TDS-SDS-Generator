namespace NanopaintHub.Infrastructure.LocalData.Repositories;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

public class DocumentReleaseRepository : IDocumentReleaseRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public DocumentReleaseRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentRelease?> GetByIdAsync(Guid id)
    {
        return await _dbContext.DocumentReleases.FindAsync(id);
    }

    public async Task<IEnumerable<DocumentRelease>> GetAllAsync()
    {
        return await _dbContext.DocumentReleases
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();
    }

    public async Task<DocumentRelease> AddAsync(DocumentRelease entity)
    {
        _dbContext.DocumentReleases.Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(DocumentRelease entity)
    {
        _dbContext.DocumentReleases.Update(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(DocumentRelease entity)
    {
        _dbContext.DocumentReleases.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<DocumentRelease>> GetReleasesByProductIdAsync(Guid productId)
    {
        return await _dbContext.DocumentReleases
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentRelease>> GetReleasesByTypeAsync(string documentType)
    {
        return await _dbContext.DocumentReleases
            .Where(r => r.DocumentType == documentType)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();
    }
}
