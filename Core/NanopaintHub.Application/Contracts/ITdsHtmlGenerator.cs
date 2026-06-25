namespace NanopaintHub.Application.Contracts;

using NanopaintHub.Application.Tds;

/// <summary>
/// Define as operações de geração de HTML/CSS a partir do esquema estrutural do builder.
/// </summary>
public interface ITdsHtmlGenerator
{
    string GenerateHtml(TemplateBuilderSchema schema);
    string GenerateCss(TemplateBuilderSchema schema);
}
