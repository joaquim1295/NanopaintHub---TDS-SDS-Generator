namespace NanopaintHub.Application.Features.SdsGeneration;

using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Exceptions;
using NanopaintHub.Domain.Entities;

// 1. O PEDIDO (Query)
public record GenerateSdsQuery(Guid ProductId, Guid TemplateId);

// 2. O EXECUTOR (UseCase)
public class GenerateSdsUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IDocumentGeneratorService _documentService;

    public GenerateSdsUseCase(
        IProductRepository productRepository,
        ITemplateRepository templateRepository,
        IDocumentGeneratorService documentService)
    {
        _productRepository = productRepository;
        _templateRepository = templateRepository;
        _documentService = documentService;
    }

    public async Task<byte[]> ExecuteAsync(GenerateSdsQuery query)
    {
        // Passo 1: Procurar o produto
        var product = await _productRepository.GetByIdAsync(query.ProductId);
        if (product == null)
        {
            throw new EntityNotFoundException(
                nameof(Product),
                query.ProductId,
                $"Produto com ID {query.ProductId} não encontrado para emissão de SDS.");
        }

        // Passo 2: Procurar o template específico da SDS (as margens, HTML base, etc.)
        var template = await _templateRepository.GetByIdAsync(query.TemplateId);
        if (template == null)
        {
            throw new EntityNotFoundException(
                nameof(DocumentTemplate),
                query.TemplateId,
                $"Template de SDS com ID {query.TemplateId} não encontrado.");
        }

        // Passo 3: Emitir os bytes do documento focado na segurança química
        byte[] sdsBytes = await _documentService.GenerateSdsAsync(product, template);

        return sdsBytes;
    }
}