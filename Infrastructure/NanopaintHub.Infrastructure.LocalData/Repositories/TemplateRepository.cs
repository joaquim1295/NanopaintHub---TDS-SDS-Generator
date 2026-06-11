namespace NanopaintHub.Infrastructure.LocalData.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;

public class TemplateRepository : ITemplateRepository
{
    private readonly NanopaintHubDbContext _dbContext;

    public TemplateRepository(NanopaintHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<DocumentTemplate?> GetByIdAsync(Guid id)
    {
        return await _dbContext.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<DocumentTemplate?> GetByTypeAsync(string documentType)
    {
        return await _dbContext.DocumentTemplates
            .FirstOrDefaultAsync(t => t.DocumentType == documentType);
    }

    public async Task<IEnumerable<DocumentTemplate>> GetAllAsync()
    {
        return await _dbContext.DocumentTemplates.ToListAsync();
    }

    // --- ESCRITA (WRITE - Sem Auto-Commit!) ---

    public async Task AddAsync(DocumentTemplate template)
    {
        await _dbContext.DocumentTemplates.AddAsync(template);
    }

    public Task UpdateAsync(DocumentTemplate template)
    {
        template.MarkAsModified();
        _dbContext.DocumentTemplates.Update(template);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var template = await GetByIdAsync(id);
        if (template != null)
        {
            _dbContext.DocumentTemplates.Remove(template);
        }
    }
}