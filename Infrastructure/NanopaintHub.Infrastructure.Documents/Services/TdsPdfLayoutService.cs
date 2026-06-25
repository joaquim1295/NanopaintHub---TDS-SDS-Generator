namespace NanopaintHub.Infrastructure.Documents.Services;

using System;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;

public sealed class TdsPdfLayoutService : IPdfLayoutService
{
    public void NormalizeLayout(TemplateBuilderSchema schema, DocumentTemplate template)
    {
        bool isTds = string.Equals(template.DocumentType, "TDS", StringComparison.OrdinalIgnoreCase);
        if (isTds && schema.Layout != null)
        {
            if (schema.Header?.HeaderRepeatStyle != "None" && schema.Layout.TopMargin < 12)
            {
                schema.Layout.TopMargin = 12;
            }
            if (schema.Footer?.FooterRepeatStyle != "None" && schema.Layout.BottomMargin < 15)
            {
                schema.Layout.BottomMargin = 15;
            }
        }
    }

    public PdfLayoutConfig GenerateLayoutConfig(DocumentTemplate template, TemplateBuilderSchema? schema, string? logoBase64, PdfGenerationSettings settings)
    {
        bool isTds = string.Equals(template.DocumentType, "TDS", StringComparison.OrdinalIgnoreCase);
        
        // Parse left/right margins from schema
        double leftMargin = schema?.Layout?.LeftMargin ?? 20;
        double rightMargin = schema?.Layout?.RightMargin ?? 20;
        double topMargin = schema?.Layout?.TopMargin ?? 45;
        double bottomMargin = schema?.Layout?.BottomMargin ?? 40;

        var config = new PdfLayoutConfig
        {
            LeftMargin = leftMargin,
            RightMargin = rightMargin,
            TopMargin = topMargin,
            BottomMargin = bottomMargin
        };

        if (isTds)
        {
            config.OmitPuppeteerMargins = true; // Use Paged.js CSS margins instead
            config.DisplayHeaderFooter = false; // Paged.js handles headers and footers natively
            config.HeaderTemplate = "<span></span>";
            config.FooterTemplate = "<span></span>";
        }
        else
        {
            // Default styling for SDS or raw HTML if schema is missing
            config.OmitPuppeteerMargins = false;
            config.DisplayHeaderFooter = true;
            config.HeaderTemplate = $"<div style=\"font-size: 10px; width: 100%; text-align: right; margin-right: 20px; color: #1f4f7d; font-weight: bold;\">NanopaintHub - {template.TemplateName}</div>";
            config.FooterTemplate = $"<div style=\"font-size: 8px; width: 100%; text-align: center; color: #888;\">Página <span class=\"pageNumber\"></span> de <span class=\"totalPages\"></span></div>";
        }

        return config;
    }
}
