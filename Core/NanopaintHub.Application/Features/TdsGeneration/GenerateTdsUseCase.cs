namespace NanopaintHub.Application.Features.TdsGeneration;

using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Exceptions;
using NanopaintHub.Domain.Entities;

// 1. O PEDIDO (Query)
public record GenerateTdsQuery(Guid ProductId, Guid TemplateId, Guid? TestBatchId = null, bool UseLatestBatchWhenUnspecified = true, PdfGenerationSettings? Settings = null);

// 2. O MAESTRO (UseCase)
public class GenerateTdsUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ITemplateRepository _templateRepository;
    
    private readonly IDocumentGeneratorService _documentService;
    private readonly ITestBatchRepository _testBatches;

    public GenerateTdsUseCase(
        IProductRepository productRepository,
        ITemplateRepository templateRepository,
        ITestBatchRepository testBatches,
        IDocumentGeneratorService documentService)
    {
        _productRepository = productRepository;
        _templateRepository = templateRepository;
        _testBatches = testBatches;
        _documentService = documentService;
    }

    public async Task<byte[]> ExecuteAsync(GenerateTdsQuery query)
    {
        var product = await _productRepository.GetByIdAsync(query.ProductId);
        if (product == null)
        {
            throw new EntityNotFoundException(
                nameof(Product),
                query.ProductId,
                $"Produto com ID {query.ProductId} não encontrado na base de dados.");
        }

        var template = await _templateRepository.GetByIdAsync(query.TemplateId);
        if (template == null)
        {
            throw new EntityNotFoundException(
                nameof(DocumentTemplate),
                query.TemplateId,
                $"Template de documento com ID {query.TemplateId} não encontrado.");
        }
        
        TestBatch? batch = null;
        if (query.TestBatchId is Guid batchId && batchId != Guid.Empty)
        {
            batch = await _testBatches.GetByIdAsync(batchId);
            if (batch is null || batch.ProductId != product.Id)
            {
                throw new EntityNotFoundException(
                    nameof(TestBatch),
                    batchId,
                    "Lote de teste não encontrado ou não pertence a este produto.");
            }
        }
        else if (query.UseLatestBatchWhenUnspecified)
        {
            batch = await _testBatches.GetLatestByProductIdAsync(product.Id);
        }

        return await _documentService.GenerateTdsAsync(product, template, batch, query.Settings);
    }
} 