using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.DTOs;
using NanopaintHub.Application.Exceptions;
using NanopaintHub.Application.Features.Products;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.UnitTests.UseCases;

public sealed class SaveProductUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenFeaturesJsonIsInvalid_ThrowsInvalidJsonPropertyException()
    {
        var useCase = new SaveProductUseCase(
            new ProductRepositoryStub(),
            new CategoryRepositoryStub(),
            new UnitOfWorkStub());

        var request = new SaveProductRequest(
            null,
            "PRD-01",
            "Produto",
            "Descricao",
            null,
            "{invalid}",
            "{}",
            "{}",
            "{}",
            "{}",
            DateTime.Today,
            1);

        var ex = await Assert.ThrowsAsync<InvalidJsonPropertyException>(() => useCase.ExecuteAsync(request));
        Assert.Equal("FeaturesJson", ex.PropertyName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryIsMissing_UsesFirstAvailableCategory()
    {
        var products = new ProductRepositorySpy();
        var categories = new CategoryRepositoryStub();
        var useCase = new SaveProductUseCase(products, categories, new UnitOfWorkStub());

        var request = new SaveProductRequest(
            null,
            "PRD-02",
            "Produto 2",
            "Descricao",
            null,
            "[]",
            "{}",
            "{}",
            "{}",
            "{}",
            DateTime.Today,
            1);

        await useCase.ExecuteAsync(request);

        Assert.NotNull(products.LastAdded);
        Assert.Equal(categories.DefaultCategoryId, products.LastAdded!.CategoryId);
    }

    private sealed class ProductRepositoryStub : IProductRepository
    {
        public Task AddAsync(Product product) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<Product>> GetAllAsync() => Task.FromResult<IEnumerable<Product>>([]);
        public Task<Product?> GetByCodeAsync(string code) => Task.FromResult<Product?>(null);
        public Task<Product?> GetByIdAsync(Guid id) => Task.FromResult<Product?>(null);
        public Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm) => Task.FromResult<IEnumerable<Product>>([]);
        public Task UpdateAsync(Product product) => Task.CompletedTask;
    }

    private sealed class ProductRepositorySpy : IProductRepository
    {
        public Product? LastAdded { get; private set; }

        public Task<Product?> GetByIdAsync(Guid id) => Task.FromResult<Product?>(null);
        public Task<Product?> GetByCodeAsync(string code) => Task.FromResult<Product?>(null);
        public Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm) => Task.FromResult<IEnumerable<Product>>([]);
        public Task<IEnumerable<Product>> GetAllAsync() => Task.FromResult<IEnumerable<Product>>([]);
        public Task AddAsync(Product product)
        {
            LastAdded = product;
            return Task.CompletedTask;
        }
        public Task UpdateAsync(Product product) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
    }

    private sealed class CategoryRepositoryStub : ICategoryRepository
    {
        public Guid DefaultCategoryId { get; } = Guid.NewGuid();

        public Task<IReadOnlyList<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductCategory>>([
                new ProductCategoryWithId(DefaultCategoryId) { Name = "Default" }
            ]);
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public Task<int> CommitAsync() => Task.FromResult(1);
    }

    private sealed class ProductCategoryWithId : ProductCategory
    {
        public ProductCategoryWithId(Guid id)
        {
            Id = id;
        }
    }
}
