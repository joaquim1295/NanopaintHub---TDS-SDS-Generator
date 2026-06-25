using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.Documents.Tds;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NanopaintHub.Infrastructure.Documents.Services;

public sealed class QuestPdfDocumentGeneratorService : IDocumentGeneratorService
{
    private readonly ISupplierComponentRepository _supplierComponents;
    private readonly IPdfGenerationProgressNotifier _progress;
    private readonly TdsDataMergeService _tdsMerge;
    private readonly ITdsChartRenderer _chartRenderer;

    public QuestPdfDocumentGeneratorService(
        ISupplierComponentRepository supplierComponents,
        IPdfGenerationProgressNotifier progress,
        TdsDataMergeService tdsMerge,
        ITdsChartRenderer chartRenderer)
    {
        _supplierComponents = supplierComponents;
        _progress = progress;
        _tdsMerge = tdsMerge;
        _chartRenderer = chartRenderer;
    }

    public Task<byte[]> GenerateTdsAsync(Product product, DocumentTemplate template, TestBatch? testBatch = null, PdfGenerationSettings? settings = null)
    {

        var featuresList = SafeDeserialize(product.FeaturesJson, new List<string>(), "FeaturesJson");
        var liquidProps = SafeDeserialize(product.LiquidPropertiesJson, new Dictionary<string, string>(), "LiquidPropertiesJson");
        var mechanicalProps = SafeDeserialize(product.MechanicalPropertiesJson, new Dictionary<string, string>(), "MechanicalPropertiesJson");
        var functionalProps = SafeDeserialize(product.FunctionalPropertiesJson, new Dictionary<string, string>(), "FunctionalPropertiesJson");
        var jsonExtendedSpecs = TdsSensorDataMerger.ParseProductSpecs(product.TdsSensorSpecsJson);

        var mergedSpecs = _tdsMerge.Merge(product, testBatch);
        var imageFallback = testBatch?.CurveGraphPath;
        var chartSvgs = _chartRenderer.RenderAll(
            mergedSpecs,
            linearityImagePath: imageFallback,
            resistanceImagePath: imageFallback,
            hysteresisImagePath: imageFallback);
        var batchNumber = _tdsMerge.ResolveBatchNumber(testBatch);

        TemplateBuilderSchema schema;
        if (!string.IsNullOrWhiteSpace(template.BuilderJson))
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            schema = JsonSerializer.Deserialize<TemplateBuilderSchema>(template.BuilderJson, options) ?? new TemplateBuilderSchema();
        }
        else
        {
            schema = new TemplateBuilderSchema
            {
                Sections = new List<TemplateSectionSchema>
                {
                    new TemplateSectionSchema { Title = "Features", Type = "BulletPoints", DataSource = "Features" },
                    new TemplateSectionSchema { Title = "Ink properties", Type = "Table", DataSource = "LiquidProperties" },
                    new TemplateSectionSchema { Title = "Handling Guidelines", Type = "Table", DataSource = "MechanicalProperties" },
                    new TemplateSectionSchema { Title = "Sensor specifications", Type = "Table", DataSource = "Tds" }
                }
            };
        }

        _progress.Report("Gerando PDF com QuestPDF (Nativo)...", 50);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(schema.Theme.FontFamily).FontSize((float)schema.Theme.BaseFontSize).FontColor("#223145"));

                page.Header().Column(col => 
                {
                    col.Item().ShowOnce().Element(c => ComposeHeader(c, schema, product, template));
                    col.Item().SkipOnce().Element(c => ComposeMinimalHeader(c, schema, product, template));
                });
                
                page.Content()
                    .PaddingHorizontal((float)schema.Layout.LeftMargin, Unit.Millimetre)
                    .Element(c => ComposeContent(c, schema, featuresList, liquidProps, mechanicalProps, mergedSpecs, chartSvgs, batchNumber));
                
                page.Footer()
                    .PaddingHorizontal((float)schema.Layout.LeftMargin, Unit.Millimetre)
                    .PaddingBottom((float)schema.Layout.BottomMargin, Unit.Millimetre)
                    .Element(c => ComposeFooter(c, schema));
            });
        });

        var bytes = document.GeneratePdf();
        _progress.Report("QuestPDF gerado com sucesso.", 100);
        return Task.FromResult(bytes);
    }

    private void ComposeHeader(IContainer container, TemplateBuilderSchema schema, Product product, DocumentTemplate template)
    {
        var primary = schema.Theme.PrimaryColor;
        var secondary = schema.Theme.SecondaryColor;
        float scale = schema.Layout.HeaderBaseScale > 0 ? (float)schema.Layout.HeaderBaseScale : 1.0f;

        container.Background(secondary) // Simplified gradient for PoC
            .PaddingHorizontal((float)schema.Layout.LeftMargin, Unit.Millimetre)
            .PaddingTop((float)schema.Layout.HeaderPaddingTop * scale, Unit.Millimetre)
            .PaddingBottom((float)schema.Layout.HeaderPaddingBottom * scale, Unit.Millimetre)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    if (schema.Header.ShowLogo)
                    {
                        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Nanopaint_logo.png");
                        if (File.Exists(logoPath))
                        {
                            row.ConstantItem(120 * scale).Image(logoPath).FitArea();
                        }
                    }

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Line(string.IsNullOrWhiteSpace(schema.Header.InfoCategory) ? "Functional Inks" : ReplacePlaceholders(schema.Header.InfoCategory, product, template))
                            .FontSize(((float)schema.Theme.BaseFontSize + 1f) * scale).Bold().FontColor("#9ac7f4").LetterSpacing(0.06f);
                        text.Line(string.IsNullOrWhiteSpace(schema.Header.InfoDocType) ? "Technical Data Sheet" : ReplacePlaceholders(schema.Header.InfoDocType, product, template))
                            .FontSize((float)schema.Theme.BaseFontSize * scale).FontColor(Colors.White);
                        text.Line($"Revised: {product.RevisionDate:yyyy-MM-dd} · Version {product.DocumentVersion}")
                            .FontSize(((float)schema.Theme.BaseFontSize - 1f) * scale).FontColor("#d5e4f5");
                    });
                });

                col.Item().PaddingTop(10 * scale).Text(string.IsNullOrWhiteSpace(schema.Header.IdentType) ? "Piezoresistive Ink" : ReplacePlaceholders(schema.Header.IdentType, product, template))
                    .FontSize((float)schema.Theme.BaseFontSize * 2f * scale).Bold().FontColor(Colors.White).LetterSpacing(0.06f);
                    
                col.Item().PaddingTop(4 * scale).Text(string.IsNullOrWhiteSpace(schema.Header.IdentName) ? product.Name : ReplacePlaceholders(schema.Header.IdentName, product, template))
                    .FontSize(((float)schema.Theme.BaseFontSize + 2f) * scale).Medium().FontColor("#b7d7f6");

                col.Item().PaddingTop(8 * scale).Text(string.IsNullOrWhiteSpace(schema.Header.IdentDesc) ? product.Description : ReplacePlaceholders(schema.Header.IdentDesc, product, template))
                    .FontSize(((float)schema.Theme.BaseFontSize + 1f) * scale).FontColor("#e6f0fb");
            });
    }

    private string ReplacePlaceholders(string text, Product product, DocumentTemplate template)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        return text
            .Replace("{{Name}}", product.Name ?? "")
            .Replace("{{Product.Name}}", product.Name ?? "")
            .Replace("{{Code}}", product.Code ?? "")
            .Replace("{{Product.Code}}", product.Code ?? "")
            .Replace("{{Description}}", product.Description ?? "")
            .Replace("{{Product.Description}}", product.Description ?? "")
            .Replace("{{TemplateName}}", template.TemplateName ?? "")
            .Replace("{{RevisionDate}}", product.RevisionDate.ToString("yyyy-MM-dd"))
            .Replace("{{Version}}", product.DocumentVersion.ToString());
    }

    private void ComposeMinimalHeader(IContainer container, TemplateBuilderSchema schema, Product product, DocumentTemplate template)
    {
        if (schema.Header.HeaderRepeatStyle == "None") return;
        
        float scale = schema.Layout.HeaderBaseScale > 0 ? (float)schema.Layout.HeaderBaseScale : 1.0f;

        container.PaddingHorizontal((float)schema.Layout.LeftMargin, Unit.Millimetre)
                 .PaddingTop((float)schema.Layout.HeaderPaddingTop * scale, Unit.Millimetre)
                 .PaddingBottom(4 * scale, Unit.Millimetre)
                 .BorderBottom(2).BorderColor(schema.Theme.PrimaryColor)
                 .PaddingBottom((float)schema.Layout.HeaderPaddingBottom * scale, Unit.Millimetre)
                 .Row(row =>
                 {
                     if (schema.Header.ShowLogo)
                     {
                         var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Nanopaint_logo.png");
                         if (File.Exists(logoPath))
                         {
                             row.ConstantItem(80 * scale).AlignBottom().Image(logoPath).FitArea();
                             row.ConstantItem(15 * scale);
                         }
                     }

                     row.RelativeItem().AlignBottom().Text(string.IsNullOrWhiteSpace(schema.Header.IdentName) ? $"{product.Name} · {product.Code}" : ReplacePlaceholders(schema.Header.IdentName, product, template))
                        .FontSize(10 * scale).Bold().FontColor(schema.Theme.PrimaryColor);
                     
                     row.RelativeItem().AlignRight().AlignBottom().Text(string.IsNullOrWhiteSpace(schema.Header.InfoDocType) ? "Technical Data Sheet" : ReplacePlaceholders(schema.Header.InfoDocType, product, template))
                        .FontSize(9 * scale).FontColor(schema.Theme.SecondaryColor);
                 });
    }

    private void ComposeContent(IContainer container, TemplateBuilderSchema schema, List<string> features, Dictionary<string, string> liquidProps, Dictionary<string, string> mechanicalProps, TdsSensorSpecs mergedSpecs, IReadOnlyDictionary<string, string> chartSvgs, string? batchNumber)
    {
        container.PaddingTop((float)schema.Layout.ContentPaddingTop, Unit.Millimetre)
                 .Column(col =>
        {
            if (!string.IsNullOrEmpty(batchNumber) && schema.Batch.ShowBatchNote)
            {
                var format = string.IsNullOrWhiteSpace(schema.Batch.BatchNoteFormat) 
                    ? "Batch data applied: {batch} (merged with product specifications where applicable)." 
                    : schema.Batch.BatchNoteFormat;
                
                var parts = format.Split("{batch}");

                col.Item().PaddingBottom((float)schema.Layout.BatchNotePaddingBottom, Unit.Millimetre).Background("#eef6fc").Border(1).BorderColor("#cde1f5").PaddingVertical(4).PaddingHorizontal(8)
                    .Text(text =>
                {
                    text.Span(parts[0]).FontSize((float)schema.Theme.BaseFontSize - 0.5f).FontColor("#1a508b");
                    if (!string.IsNullOrEmpty(batchNumber))
                    {
                        text.Span(batchNumber).FontSize((float)schema.Theme.BaseFontSize - 0.5f).Bold().FontColor("#1a508b");
                        if (parts.Length > 1) text.Span(parts[1]).FontSize((float)schema.Theme.BaseFontSize - 0.5f).FontColor("#1a508b");
                    }
                });
            }

            col.Spacing(15);

            if (schema.Sections == null || schema.Sections.Count == 0)
            {
                schema.Sections = new List<TemplateSectionSchema>
                {
                    new TemplateSectionSchema { Title = "Ink features", Type = "BulletPoints", DataSource = "Features", IsHalfWidth = true },
                    new TemplateSectionSchema { Title = "Ink properties", Type = "Table", DataSource = "LiquidProperties", IsHalfWidth = true },
                    new TemplateSectionSchema { Title = "Handling Guidelines", Type = "Table", DataSource = "MechanicalProperties", IsHalfWidth = false },
                    new TemplateSectionSchema { Title = "Sensor specifications", Type = "Table", DataSource = "Tds", IsHalfWidth = false },
                    new TemplateSectionSchema { Title = "Performance charts", Type = "Charts", DataSource = "Charts", IsHalfWidth = false }
                };
            }

            var halfWidthQueue = new List<TemplateSectionSchema>();

            void FlushHalfWidthQueue()
            {
                if (halfWidthQueue.Count > 0)
                {
                    col.Item().Row(row =>
                    {
                        // Left Column
                        row.RelativeItem().Column(leftCol =>
                        {
                            for (int i = 0; i < halfWidthQueue.Count; i += 2)
                            {
                                var sec = halfWidthQueue[i];
                                leftCol.Item().PaddingBottom((float)schema.Layout.SectionSpacing, Unit.Millimetre).ShowEntire().Column(inner => RenderSection(inner, sec, schema, features, liquidProps, mechanicalProps, mergedSpecs, chartSvgs));
                            }
                        });

                        row.ConstantItem(10); // Column Gap

                        // Right Column
                        row.RelativeItem().Column(rightCol =>
                        {
                            for (int i = 1; i < halfWidthQueue.Count; i += 2)
                            {
                                var sec = halfWidthQueue[i];
                                rightCol.Item().PaddingBottom((float)schema.Layout.SectionSpacing, Unit.Millimetre).ShowEntire().Column(inner => RenderSection(inner, sec, schema, features, liquidProps, mechanicalProps, mergedSpecs, chartSvgs));
                            }
                        });
                    });
                    halfWidthQueue.Clear();
                }
            }

            foreach (var sec in schema.Sections)
            {
                if (sec.IsHalfWidth)
                {
                    halfWidthQueue.Add(sec);
                }
                else
                {
                    FlushHalfWidthQueue();
                    col.Item().PaddingBottom((float)schema.Layout.SectionSpacing, Unit.Millimetre).ShowEntire().Column(inner => RenderSection(inner, sec, schema, features, liquidProps, mechanicalProps, mergedSpecs, chartSvgs));
                }
            }

            FlushHalfWidthQueue();
        });
    }

    private void RenderSection(ColumnDescriptor inner, TemplateSectionSchema sec, TemplateBuilderSchema schema, List<string> features, Dictionary<string, string> liquidProps, Dictionary<string, string> mechanicalProps, TdsSensorSpecs mergedSpecs, IReadOnlyDictionary<string, string> chartSvgs)
    {
        // Legacy fallback: Convert old RichText HTML tables into native QuestPDF tables
        if (sec.Type == "RichText")
        {
            if (sec.Content != null && sec.Content.Contains("{{Tds."))
            {
                sec.Type = "Table";
                sec.DataSource = "Tds";
            }
            else if (sec.Title == "Handling Guidelines" || (sec.Content != null && sec.Content.Contains("Processing</td>")))
            {
                sec.Type = "Table";
                sec.DataSource = "MechanicalProperties";
            }
            else if (sec.Title == "Ink properties" || (sec.Content != null && sec.Content.Contains("Solid content</td>")))
            {
                sec.Type = "Table";
                sec.DataSource = "LiquidProperties";
            }
        }

        float scale = schema.Layout.SectionBaseScale > 0 ? (float)schema.Layout.SectionBaseScale : 1.0f;
        float baseFontSize = sec.FontSize > 0 ? (float)sec.FontSize : (float)schema.Theme.BaseFontSize * scale;

        inner.Item().PaddingBottom(6).Element(c => DrawSectionTitle(c, sec.Title, schema.Theme.SecondaryColor, baseFontSize));

        if (sec.Type == "BulletPoints")
        {
            var data = sec.DataSource == "Features" ? features : new List<string>();
            foreach (var f in data)
            {
                inner.Item().Row(fr =>
                {
                    fr.ConstantItem(15).Text("✓").FontColor(schema.Theme.PrimaryColor).Bold();
                    fr.RelativeItem().Text(f).FontSize(baseFontSize);
                });
            }
        }
        else if (sec.Type == "Table")
        {
            Dictionary<string, string> data = new();
            if (sec.DataSource == "LiquidProperties") data = liquidProps;
            else if (sec.DataSource == "MechanicalProperties") data = mechanicalProps;
            else if (sec.DataSource == "Tds")
            {
                data = new Dictionary<string, string>
                {
                    { "Sensitivity (Volts)", $"{mergedSpecs.SensitivityVolts} V" },
                    { "Sensitivity (Per Unit)", $"{mergedSpecs.SensitivityPerUnit} V/{mergedSpecs.ForceUnit}" },
                    { "Response time", $"{mergedSpecs.ResponseTimeSec} s" },
                    { "Recovery time", $"{mergedSpecs.RecoveryTimeSec} s" },
                    { "Repeatability", $"{mergedSpecs.RepeatabilityPercent} %" },
                    { "Rest resistance", $"{mergedSpecs.RestResistanceOhms} Ω" },
                    { "Operating temperature", $"{mergedSpecs.OperatingTempMinC} °C to {mergedSpecs.OperatingTempMaxC} °C" },
                    { "Range", $"{mergedSpecs.RangeMin} - {mergedSpecs.RangeMax} {mergedSpecs.ForceUnit}" },
                    { "Hysteresis", $"{mergedSpecs.HysteresisPercent} %" }
                };
            }
            DrawTable(inner, data, schema.Theme.PrimaryColor, baseFontSize);
        }
        else if (sec.Type == "Charts")
        {
            inner.Item().Row(r =>
            {
                if (chartSvgs.TryGetValue("linearity", out var linearitySvg))
                {
                    r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => {
                        c.Item().Text("Linearity").Bold().FontSize(baseFontSize).FontColor(schema.Theme.SecondaryColor);
                        c.Item().Height(100).Background(Colors.Grey.Lighten4).AlignCenter().AlignMiddle().Text("[Chart SVG Area]");
                    });
                }
                r.ConstantItem(10);
                if (chartSvgs.TryGetValue("resistance", out var resistanceSvg))
                {
                    r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => {
                        c.Item().Text("Resistance").Bold().FontSize(baseFontSize).FontColor(schema.Theme.SecondaryColor);
                        c.Item().Height(100).Background(Colors.Grey.Lighten4).AlignCenter().AlignMiddle().Text("[Chart SVG Area]");
                    });
                }
            });
        }
        else if (sec.Type == "RichText")
        {
            // Basic HTML stripping fallback
            var text = System.Text.RegularExpressions.Regex.Replace(sec.Content ?? "", "<[^>]*>", string.Empty);
            inner.Item().Text(text).FontSize(baseFontSize);
        }
    }

    private void DrawSectionTitle(IContainer container, string title, string color, float baseFontSize)
    {
        container.PaddingBottom(5).BorderBottom(2).BorderColor("#d8e1ec").Text(title.ToUpperInvariant())
            .FontSize(baseFontSize + 1f).Bold().LetterSpacing(0.07f).FontColor(color);
    }

    private void DrawTable(ColumnDescriptor col, Dictionary<string, string> data, string primaryColor, float baseFontSize)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            bool isEven = false;
            foreach (var kvp in data)
            {
                var bg = isEven ? "#f6f9fc" : "#ffffff";

                table.Cell().Border(1).BorderColor("#d8e1ec").Background(bg).Padding(4).Text(kvp.Key).FontSize(baseFontSize).Bold().FontColor(primaryColor);
                table.Cell().Border(1).BorderColor("#d8e1ec").Background(bg).Padding(4).Text(kvp.Value).FontSize(baseFontSize);

                isEven = !isEven;
            }
        });
    }

    private void ComposeFooter(IContainer container, TemplateBuilderSchema schema)
    {
        container.BorderTop(1).BorderColor("#d8e1ec").PaddingTop(4, Unit.Millimetre).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(string.IsNullOrWhiteSpace(schema.Footer.Column1) ? "NanoPaint, Lda.\nUniversidade do Minho\nBraga, Portugal" : schema.Footer.Column1)
                   .FontSize((float)schema.Theme.BaseFontSize - 1f).Bold().FontColor(schema.Theme.PrimaryColor);
                
                row.RelativeItem().AlignRight().Text(string.IsNullOrWhiteSpace(schema.Footer.Column2) ? "www.nanopaint-tech.com\ninfo@nanopaint-tech.com" : schema.Footer.Column2)
                   .FontSize((float)schema.Theme.BaseFontSize - 1f).Bold().FontColor(schema.Theme.PrimaryColor);
            });

            var defaultLegal = "© 2026 NanoPaint Lda. All rights reserved. This Technical Data Sheet is generated from product records in NanopaintHub. Values shown reflect stored product data with optional batch overlay.";
            col.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(schema.Footer.LegalText) ? defaultLegal : schema.Footer.LegalText)
               .FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Medium);
            
            col.Item().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                x.CurrentPageNumber().FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                x.Span(" of ").FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                x.TotalPages().FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    public async Task<byte[]> GenerateSdsAsync(Product product, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        _progress.Report("Carregando componentes para SDS...", 20);
        var rows = await _supplierComponents.GetAllWithSupplierAsync();
        var components = rows.Select(r => new { r.SupplierName, r.ComponentName, r.CasNumber }).ToList();

        _progress.Report("Gerando matriz GHS SDS com QuestPDF...", 50);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Text($"Safety Data Sheet (SDS)\n{product.Name}").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                    row.ConstantItem(100).AlignRight().Text($"Rev: {product.RevisionDate:dd/MM/yyyy}\nVersion: {product.DocumentVersion}").FontSize(9);
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Spacing(10);
                    
                    // Seção 1
                    col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text("1. Identification").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Product Name: {product.Name}");
                    col.Item().Text($"Product Code: {product.Code}");
                    col.Item().Text($"Description: {product.Description}");

                    // Seção 2
                    col.Item().PaddingTop(10).PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text("2. Hazard Identification").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Classified according to GHS standards.");

                    // Seção 3
                    col.Item().PaddingTop(10).PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Text("3. Composition / Information on Ingredients").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Component Name").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("CAS Number").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Supplier").Bold();
                        });

                        foreach (var comp in components)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(comp.ComponentName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(comp.CasNumber);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(comp.SupplierName);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        var bytes = document.GeneratePdf();
        _progress.Report("SDS QuestPDF gerado com sucesso.", 100);
        return bytes;
    }

    private static T SafeDeserialize<T>(string? json, T fallback, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;
        try
        {
            return JsonSerializer.Deserialize<T>(json) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
