using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.DTOs;
using NanopaintHub.Application.Features.Templates;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.UnitTests.UseCases;

public sealed class SaveDocumentTemplateUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDocumentTypeIsInvalid_ThrowsInvalidOperationException()
    {
        var useCase = new SaveDocumentTemplateUseCase(new TemplateRepositoryStub(), new UnitOfWorkStub());
        var request = new SaveDocumentTemplateRequest(
            null,
            "T-1",
            "ABC",
            string.Empty,
            10,
            10,
            "<html></html>");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request));
        Assert.Contains("DocumentType", ex.Message);
    }

    private sealed class TemplateRepositoryStub : ITemplateRepository
    {
        public Task AddAsync(DocumentTemplate template) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<DocumentTemplate>> GetAllAsync() => Task.FromResult<IEnumerable<DocumentTemplate>>([]);
        public Task<DocumentTemplate?> GetByIdAsync(Guid id) => Task.FromResult<DocumentTemplate?>(null);
        public Task<DocumentTemplate?> GetByTypeAsync(string documentType) => Task.FromResult<DocumentTemplate?>(null);
        public Task UpdateAsync(DocumentTemplate template) => Task.CompletedTask;
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public Task<int> CommitAsync() => Task.FromResult(1);
    }
}
