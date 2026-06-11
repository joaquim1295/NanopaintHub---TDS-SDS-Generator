namespace NanopaintHub.Application.Contracts;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface ITemplateRepository
{
    Task<DocumentTemplate?> GetByIdAsync(Guid id);
    Task<DocumentTemplate?> GetByTypeAsync(string documentType);
    Task<IEnumerable<DocumentTemplate>> GetAllAsync();
    
    Task AddAsync(DocumentTemplate template);
    Task UpdateAsync(DocumentTemplate template);
    Task DeleteAsync(Guid id);
}