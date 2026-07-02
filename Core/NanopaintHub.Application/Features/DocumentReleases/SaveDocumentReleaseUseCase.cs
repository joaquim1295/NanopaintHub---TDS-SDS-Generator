namespace NanopaintHub.Application.Features.DocumentReleases;

using System;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

public record SaveDocumentReleaseCommand(
    Guid ProductId, 
    string ProductName, 
    string DocumentType, 
    string Version, 
    string FilePath, 
    string SnapshotJson);

public class SaveDocumentReleaseUseCase
{
    private readonly IDocumentReleaseRepository _repository;

    public SaveDocumentReleaseUseCase(IDocumentReleaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<DocumentRelease> ExecuteAsync(SaveDocumentReleaseCommand command)
    {
        var release = new DocumentRelease
        {
            ProductId = command.ProductId,
            ProductName = command.ProductName,
            DocumentType = command.DocumentType,
            Version = command.Version,
            FilePath = command.FilePath,
            SnapshotJson = command.SnapshotJson,
            GeneratedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(release);
    }
}
