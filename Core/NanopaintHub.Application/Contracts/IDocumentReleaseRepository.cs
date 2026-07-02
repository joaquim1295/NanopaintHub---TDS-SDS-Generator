namespace NanopaintHub.Application.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface IDocumentReleaseRepository
{
    Task<DocumentRelease?> GetByIdAsync(Guid id);
    Task<IEnumerable<DocumentRelease>> GetAllAsync();
    
    Task<DocumentRelease> AddAsync(DocumentRelease entity);
    Task UpdateAsync(DocumentRelease entity);
    Task DeleteAsync(DocumentRelease entity);
    
    Task<IEnumerable<DocumentRelease>> GetReleasesByProductIdAsync(Guid productId);
    Task<IEnumerable<DocumentRelease>> GetReleasesByTypeAsync(string documentType);
}
