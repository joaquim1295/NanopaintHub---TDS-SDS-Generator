namespace NanopaintHub.Application.DTOs;

public sealed record SaveDocumentTemplateRequest(
    Guid? Id,
    string TemplateName,
    string DocumentType,
    string LetterheadPdfPath,
    int TopMargin,
    int BottomMargin,
    string HtmlStructure);
