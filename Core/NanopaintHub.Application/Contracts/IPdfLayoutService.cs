namespace NanopaintHub.Application.Contracts;

using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;

public interface IPdfLayoutService
{
    void NormalizeLayout(TemplateBuilderSchema schema, DocumentTemplate template);
    PdfLayoutConfig GenerateLayoutConfig(DocumentTemplate template, TemplateBuilderSchema? schema, string? logoBase64, PdfGenerationSettings settings);
}
