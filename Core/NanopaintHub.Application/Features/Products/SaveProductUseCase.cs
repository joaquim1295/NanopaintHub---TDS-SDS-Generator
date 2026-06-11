using System.Linq;
using NanopaintHub.Application.Common;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.DTOs;
using NanopaintHub.Application.Exceptions;
using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Features.Products;

public sealed class SaveProductUseCase
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;

    public SaveProductUseCase(
        IProductRepository products,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        JsonValidation.EnsureJsonArray(request.FeaturesJson, nameof(request.FeaturesJson));
        JsonValidation.EnsureJsonObject(request.LiquidPropertiesJson, nameof(request.LiquidPropertiesJson));
        JsonValidation.EnsureJsonObject(request.MechanicalPropertiesJson, nameof(request.MechanicalPropertiesJson));
        JsonValidation.EnsureJsonObject(request.FunctionalPropertiesJson, nameof(request.FunctionalPropertiesJson));
        JsonValidation.EnsureJsonObject(request.TdsSensorSpecsJson, nameof(request.TdsSensorSpecsJson));
        TdsSensorSpecsValidator.Validate(TdsSensorDataMerger.ParseProductSpecs(request.TdsSensorSpecsJson));

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException("O código do produto é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("O nome do produto é obrigatório.");
        }

        var categoryId = request.CategoryId;
        if (categoryId is null || categoryId == Guid.Empty)
        {
            var all = await _categories.GetAllAsync(cancellationToken);
            var first = all.FirstOrDefault()
                        ?? throw new InvalidOperationException(
                            "Não existe nenhuma categoria. Execute o seed ou crie uma categoria antes de gravar produtos.");
            categoryId = first.Id;
        }
        else
        {
            var exists = await _categories.GetAllAsync(cancellationToken);
            if (exists.All(c => c.Id != categoryId))
            {
                throw new InvalidOperationException("Categoria seleccionada inválida.");
            }
        }

        if (request.Id is null || request.Id == Guid.Empty)
        {
            var duplicate = await _products.GetByCodeAsync(request.Code);
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"Já existe um produto com o código «{request.Code}».");
            }

            var product = new Product
            {
                Code = request.Code.Trim(),
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                CategoryId = categoryId.Value,
                FeaturesJson = request.FeaturesJson.Trim(),
                LiquidPropertiesJson = request.LiquidPropertiesJson.Trim(),
                MechanicalPropertiesJson = request.MechanicalPropertiesJson.Trim(),
                FunctionalPropertiesJson = request.FunctionalPropertiesJson.Trim(),
                TdsSensorSpecsJson = request.TdsSensorSpecsJson.Trim(),
                RevisionDate = request.RevisionDate.Date,
                DocumentVersion = request.DocumentVersion
            };
            await _products.AddAsync(product);
            await _unitOfWork.CommitAsync();
            return product.Id;
        }

        var existing = await _products.GetByIdAsync(request.Id.Value);
        if (existing is null)
        {
            throw new EntityNotFoundException(nameof(Product), request.Id, "Produto não encontrado para actualização.");
        }

        var other = await _products.GetByCodeAsync(request.Code);
        if (other is not null && other.Id != existing.Id)
        {
            throw new InvalidOperationException($"Já existe outro produto com o código «{request.Code}».");
        }

        existing.Code = request.Code.Trim();
        existing.Name = request.Name.Trim();
        existing.Description = request.Description.Trim();
        existing.CategoryId = categoryId.Value;
        existing.FeaturesJson = request.FeaturesJson.Trim();
        existing.LiquidPropertiesJson = request.LiquidPropertiesJson.Trim();
        existing.MechanicalPropertiesJson = request.MechanicalPropertiesJson.Trim();
        existing.FunctionalPropertiesJson = request.FunctionalPropertiesJson.Trim();
        existing.TdsSensorSpecsJson = request.TdsSensorSpecsJson.Trim();
        existing.RevisionDate = request.RevisionDate.Date;
        existing.DocumentVersion = request.DocumentVersion;

        await _products.UpdateAsync(existing);
        await _unitOfWork.CommitAsync();
        return existing.Id;
    }
}
