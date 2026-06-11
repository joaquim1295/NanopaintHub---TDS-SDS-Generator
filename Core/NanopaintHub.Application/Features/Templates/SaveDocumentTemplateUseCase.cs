using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.DTOs;
using NanopaintHub.Application.Exceptions;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Features.Templates;

public sealed class SaveDocumentTemplateUseCase
{
    private readonly ITemplateRepository _templates;
    private readonly IUnitOfWork _unitOfWork;

    public SaveDocumentTemplateUseCase(ITemplateRepository templates, IUnitOfWork unitOfWork)
    {
        _templates = templates;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(SaveDocumentTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateName))
        {
            throw new InvalidOperationException("O nome do template é obrigatório.");
        }

        var docType = request.DocumentType.Trim();
        if (!string.Equals(docType, "TDS", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(docType, "SDS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DocumentType deve ser TDS ou SDS.");
        }

        docType = string.Equals(docType, "SDS", StringComparison.OrdinalIgnoreCase) ? "SDS" : "TDS";

        if (string.IsNullOrWhiteSpace(request.HtmlStructure))
        {
            throw new InvalidOperationException("O HTML do template é obrigatório.");
        }

        if (request.Id is null || request.Id == Guid.Empty)
        {
            var template = new DocumentTemplate
            {
                TemplateName = request.TemplateName.Trim(),
                DocumentType = docType,
                LetterheadPdfPath = request.LetterheadPdfPath?.Trim() ?? string.Empty,
                TopMargin = request.TopMargin,
                BottomMargin = request.BottomMargin,
                HtmlStructure = request.HtmlStructure
            };
            await _templates.AddAsync(template);
            await _unitOfWork.CommitAsync();
            return template.Id;
        }

        var existing = await _templates.GetByIdAsync(request.Id.Value);
        if (existing is null)
        {
            throw new EntityNotFoundException(nameof(DocumentTemplate), request.Id, "Template não encontrado para actualização.");
        }

        existing.TemplateName = request.TemplateName.Trim();
        existing.DocumentType = docType;
        existing.LetterheadPdfPath = request.LetterheadPdfPath?.Trim() ?? string.Empty;
        existing.TopMargin = request.TopMargin;
        existing.BottomMargin = request.BottomMargin;
        existing.HtmlStructure = request.HtmlStructure;

        await _templates.UpdateAsync(existing);
        await _unitOfWork.CommitAsync();
        return existing.Id;
    }
}
