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
    private readonly IPdfGenerationProgressNotifier _progress;
    private readonly TdsDataMergeService _tdsMerge;
    private readonly ITdsChartRenderer _chartRenderer;
    private readonly Microsoft.Extensions.Options.IOptionsMonitor<NanopaintHub.Domain.Common.CompanyInfoOptions> _companyInfoOptions;
    private readonly IClpPhraseRepository _phraseRepo;

    public QuestPdfDocumentGeneratorService(
        IPdfGenerationProgressNotifier progress,
        TdsDataMergeService tdsMerge,
        ITdsChartRenderer chartRenderer,
        Microsoft.Extensions.Options.IOptionsMonitor<NanopaintHub.Domain.Common.CompanyInfoOptions> companyInfoOptions,
        IClpPhraseRepository phraseRepo)
    {
        _progress = progress;
        _tdsMerge = tdsMerge;
        _chartRenderer = chartRenderer;
        _companyInfoOptions = companyInfoOptions;
        _phraseRepo = phraseRepo;
    }

    private float SafeMargin(double margin, float min = 0f, float max = 80f)
    {
        return Math.Clamp((float)margin, min, max);
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
                    .PaddingLeft(SafeMargin(schema.Layout.LeftMargin), Unit.Millimetre)
                    .PaddingRight(SafeMargin(schema.Layout.RightMargin), Unit.Millimetre)
                    .Element(c => ComposeContent(c, schema, featuresList, liquidProps, mechanicalProps, mergedSpecs, chartSvgs, batchNumber));
                
                page.Footer()
                    .PaddingLeft(SafeMargin(schema.Layout.LeftMargin), Unit.Millimetre)
                    .PaddingRight(SafeMargin(schema.Layout.RightMargin), Unit.Millimetre)
                    .PaddingBottom((float)schema.Layout.BottomMargin, Unit.Millimetre)
                    .Element(c => ComposeFooter(c, schema, product, template));
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
        
        var bgColor = !string.IsNullOrWhiteSpace(schema.Header.HeaderBackgroundColor) ? schema.Header.HeaderBackgroundColor : primary;

        container.Background(bgColor) // Changed to support HeaderBackgroundColor
            .PaddingLeft(SafeMargin(schema.Layout.HeaderPaddingLeft), Unit.Millimetre)
            .PaddingRight(SafeMargin(schema.Layout.HeaderPaddingRight), Unit.Millimetre)
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
                            row.AutoItem().MaxWidth(160 * scale).Height((float)schema.Header.LogoHeight * scale).Image(logoPath).FitArea();
                        }
                    }

                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        var categoryText = string.IsNullOrWhiteSpace(product.Category) ? "Functional Inks" : product.Category;
                            
                        text.Line(categoryText)
                            .FontFamily(schema.Header.SubtitleFontFamily).FontSize(((float)schema.Theme.BaseFontSize + 1f) * scale).Bold().FontColor(schema.Header.SubtitleColor).LetterSpacing(0.06f);
                        
                        var docTypeText = "Technical Data Sheet";
                            
                        text.Line(docTypeText)
                            .FontFamily(schema.Header.SubtitleFontFamily).FontSize((float)schema.Theme.BaseFontSize * scale).FontColor(Colors.White);
                            
                        text.Line($"Revised: {product.RevisionDate:yyyy-MM-dd} · Version {product.DocumentVersion}")
                            .FontFamily(schema.Header.SubtitleFontFamily).FontSize(((float)schema.Theme.BaseFontSize - 1f) * scale).FontColor(schema.Header.DescriptionColor);
                    });
                });

                var productTypeText = string.IsNullOrWhiteSpace(product.ProductType) ? "Piezoresistive Ink" : product.ProductType;
                    
                col.Item().PaddingTop(10 * scale).Text(productTypeText)
                    .FontFamily(schema.Header.TitleFontFamily).FontSize((float)schema.Theme.BaseFontSize * 2f * scale).Bold().FontColor(schema.Header.TitleColor).LetterSpacing(0.06f);
                    
                var productNameText = product.Name;

                col.Item().PaddingTop(4 * scale).Text(productNameText)
                    .FontFamily(schema.Header.TitleFontFamily).FontSize(((float)schema.Theme.BaseFontSize + 2f) * scale).Medium().FontColor(schema.Header.SubtitleColor);

                var productDescText = product.Description;

                col.Item().PaddingTop(8 * scale).Text(productDescText)
                    .FontFamily(schema.Header.SubtitleFontFamily).FontSize(((float)schema.Theme.BaseFontSize + 1f) * scale).FontColor(schema.Header.DescriptionColor);
            });
    }

    private string ReplacePlaceholders(string text, Product product, DocumentTemplate? template = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var companyInfo = _companyInfoOptions.CurrentValue;

        return text
            .Replace("{{Name}}", product.Name ?? "")
            .Replace("{{Product.Name}}", product.Name ?? "")
            .Replace("{{Code}}", product.Code ?? "")
            .Replace("{{Product.Code}}", product.Code ?? "")
            .Replace("{{Description}}", product.Description ?? "")
            .Replace("{{Product.Description}}", product.Description ?? "")
            .Replace("{{TemplateName}}", template?.TemplateName ?? "")
            .Replace("{{RevisionDate}}", product.RevisionDate.ToString("yyyy-MM-dd"))
            .Replace("{{Date}}", product.RevisionDate.ToString("yyyy-MM-dd"))
            .Replace("{{Version}}", product.DocumentVersion.ToString())
            .Replace("{{Company.Name}}", companyInfo.Name ?? "")
            .Replace("{{Company.Address}}", companyInfo.Address ?? "")
            .Replace("{{Company.Telephone}}", companyInfo.Telephone ?? "")
            .Replace("{{Company.Email}}", companyInfo.Email ?? "")
            .Replace("{{Company.EmergencyTelephone}}", !string.IsNullOrWhiteSpace(companyInfo.EmergencyTelephone) ? companyInfo.EmergencyTelephone : (companyInfo.Telephone ?? ""))
            .Replace("{{Company.ResponsiblePerson}}", companyInfo.ResponsiblePerson ?? "");
    }

    private void ComposeMinimalHeader(IContainer container, TemplateBuilderSchema schema, Product product, DocumentTemplate template)
    {
        if (schema.Header.HeaderRepeatStyle == "None") return;
        
        float scale = schema.Layout.HeaderBaseScale > 0 ? (float)schema.Layout.HeaderBaseScale : 1.0f;

        container.PaddingTop(SafeMargin(schema.Layout.TopMargin) * scale, Unit.Millimetre)
                 .PaddingLeft(SafeMargin(schema.Layout.LeftMargin), Unit.Millimetre)
                 .PaddingRight(SafeMargin(schema.Layout.RightMargin), Unit.Millimetre)
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
                             row.AutoItem().MaxWidth(100 * scale).Height((float)schema.Header.LogoHeight * 0.75f * scale).AlignBottom().Image(logoPath).FitArea();
                             row.ConstantItem(15 * scale);
                         }
                     }

                     row.RelativeItem().AlignBottom().Text($"{product.Name} · {product.Code}")
                        .FontSize(10 * scale).Bold().FontColor(schema.Theme.PrimaryColor);
                     
                     row.RelativeItem().AlignRight().AlignBottom().Text("Technical Data Sheet")
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

    private void ComposeFooter(IContainer container, TemplateBuilderSchema schema, Product product, DocumentTemplate template)
    {
        var companyInfo = _companyInfoOptions.CurrentValue;

        container.BorderTop(1).BorderColor("#d8e1ec").PaddingTop(4, Unit.Millimetre).Column(col =>
        {
            var footerTextColor = string.IsNullOrWhiteSpace(schema.Footer.TextColor) ? schema.Theme.PrimaryColor : schema.Footer.TextColor;
            var footerFontFamily = string.IsNullOrWhiteSpace(schema.Footer.FontFamily) ? schema.Theme.FontFamily : schema.Footer.FontFamily;

            col.Item().Row(row =>
            {
                var col1Text = string.IsNullOrWhiteSpace(schema.Footer.Column1) ? $"{companyInfo.Name}\n{companyInfo.Address}" : schema.Footer.Column1;
                var col2Text = string.IsNullOrWhiteSpace(schema.Footer.Column2) ? "www.nanopaint-tech.com\ninfo@nanopaint-tech.com" : schema.Footer.Column2;

                row.RelativeItem().Text(ReplacePlaceholders(col1Text, product, template))
                   .FontFamily(footerFontFamily).FontSize((float)schema.Theme.BaseFontSize - 1f).Bold().FontColor(footerTextColor);
                
                row.RelativeItem().AlignRight().Text(ReplacePlaceholders(col2Text, product, template))
                   .FontFamily(footerFontFamily).FontSize((float)schema.Theme.BaseFontSize - 1f).Bold().FontColor(footerTextColor);
            });

            var defaultLegal = "© 2026 NanoPaint Lda. All rights reserved. This Technical Data Sheet is generated from product records in NanopaintHub. Values shown reflect stored product data with optional batch overlay.";
            var legalText = string.IsNullOrWhiteSpace(schema.Footer.LegalText) ? defaultLegal : schema.Footer.LegalText;
            col.Item().PaddingTop(2).Text(ReplacePlaceholders(legalText, product, template))
               .FontFamily(footerFontFamily).FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Medium);
            
            col.Item().Row(row =>
            {
                row.RelativeItem().AlignLeft().Text(x =>
                {
                    var ver = product.DocumentVersion.ToString();
                    var dat = product.RevisionDate.ToString("yyyy-MM-dd");
                    
                    x.Span($"Versão: {ver}").FontFamily(footerFontFamily).FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                    x.Span("   |   ").FontFamily(footerFontFamily).FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Lighten1);
                    x.Span($"Data: {dat}").FontFamily(footerFontFamily).FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                    x.CurrentPageNumber().FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                    x.Span(" of ").FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                    x.TotalPages().FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }
    public async Task<byte[]> GenerateSdsAsync(Product product, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        _progress.Report("Carregando formulação para SDS...", 20);
        
        var compositions = product.Compositions?.ToList() ?? new List<ProductComposition>();
        
        var uniqueHazards = compositions
            .Where(c => c.EchaSubstance?.Hazards != null)
            .SelectMany(c => c.EchaSubstance!.Hazards!)
            .GroupBy(h => h.PhraseCode)
            .Select(g => g.First())
            .ToList();
            
        var pCodes = string.IsNullOrWhiteSpace(product.PrecautionaryCodesJson) ? new List<string>() : (System.Text.Json.JsonSerializer.Deserialize<List<string>>(product.PrecautionaryCodesJson) ?? new List<string>());
        var allPhrases = await _phraseRepo.GetAllAsync();
        var pPhrases = allPhrases.Where(p => p.PhraseType == "P" && pCodes.Contains(p.Code)).ToList();

        var hazardPhrases = uniqueHazards
            .Select(h => $"{h.PhraseCode} {(h.Phrase?.Description ?? "Hazard description not available")}")
            .ToList();
            
        var hazardCodes = uniqueHazards.Select(h => h.PhraseCode ?? "").ToHashSet();

        var liquidProps = SafeDeserialize(product.LiquidPropertiesJson, new Dictionary<string, string>(), "LiquidPropertiesJson");

        TemplateBuilderSchema schema;
        if (!string.IsNullOrWhiteSpace(template.BuilderJson))
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            schema = JsonSerializer.Deserialize<TemplateBuilderSchema>(template.BuilderJson, options) ?? new TemplateBuilderSchema();
        }
        else
        {
            schema = new TemplateBuilderSchema();
        }



        _progress.Report("Gerando matriz GHS SDS com QuestPDF...", 50);

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

                page.Content().PaddingLeft(SafeMargin(schema.Layout.LeftMargin), Unit.Millimetre).PaddingRight(SafeMargin(schema.Layout.RightMargin), Unit.Millimetre).PaddingTop((float)schema.Layout.ContentPaddingTop, Unit.Millimetre).Column(col =>
                {
                col.Spacing(8);

                    if (schema.Sections == null || schema.Sections.Count == 0)
                    {
                        // Fallback Se o Builder não tiver gerado secções
                        ComposeSdsSection1(col, product, schema);
                        ComposeSdsSection2(col, uniqueHazards, hazardCodes, pPhrases, schema);
                        ComposeSdsSection3(col, compositions, schema);
                        ComposeSdsSection4(col, hazardCodes, product.SdsSection4CustomText, schema);
                        ComposeSdsSection5(col, hazardCodes, schema);
                        ComposeSdsSection6(col, schema);
                        ComposeSdsSection7(col, hazardCodes, product.SdsSection7CustomText, schema);
                        ComposeSdsSection8(col, schema);
                        ComposeSdsSection9(col, liquidProps, schema);
                        ComposeSdsSection10(col, schema);
                        ComposeSdsSection11(col, hazardCodes, schema);
                        ComposeSdsSection12(col, hazardCodes, schema);
                        ComposeSdsSection13(col, schema);
                        ComposeSdsSection14(col, hazardCodes, schema);
                        ComposeSdsSection15(col, schema);
                        ComposeSdsSection16(col, hazardPhrases, schema);
                    }
                    else
                    {
                        var halfWidthQueue = new List<TemplateSectionSchema>();
            
                        void FlushHalfWidthQueue()
                        {
                            if (halfWidthQueue.Count > 0)
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(leftCol =>
                                    {
                                        for (int i = 0; i < halfWidthQueue.Count; i += 2)
                                        {
                                            var sec = halfWidthQueue[i];
                                            leftCol.Item().PaddingBottom((float)schema.Layout.SectionSpacing, Unit.Millimetre).ShowEntire().Column(inner => RenderSdsSection(inner, sec, schema, product, uniqueHazards, hazardCodes, compositions, liquidProps, hazardPhrases, pPhrases));
                                        }
                                    });
                                    row.ConstantItem(10);
                                    row.RelativeItem().Column(rightCol =>
                                    {
                                        for (int i = 1; i < halfWidthQueue.Count; i += 2)
                                        {
                                            var sec = halfWidthQueue[i];
                                            rightCol.Item().PaddingBottom((float)schema.Layout.SectionSpacing, Unit.Millimetre).ShowEntire().Column(inner => RenderSdsSection(inner, sec, schema, product, uniqueHazards, hazardCodes, compositions, liquidProps, hazardPhrases, pPhrases));
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
                                col.Item().PaddingBottom((float)schema.Layout.SectionSpacing, Unit.Millimetre).ShowEntire().Column(inner => RenderSdsSection(inner, sec, schema, product, uniqueHazards, hazardCodes, compositions, liquidProps, hazardPhrases, pPhrases));
                            }
                        }
            
                        FlushHalfWidthQueue();
                    }
                });

                page.Footer()
                    .PaddingLeft(SafeMargin(schema.Layout.LeftMargin), Unit.Millimetre)
                    .PaddingRight(SafeMargin(schema.Layout.RightMargin), Unit.Millimetre)
                    .PaddingBottom((float)schema.Layout.BottomMargin, Unit.Millimetre)
                    .Element(c => ComposeFooter(c, schema, product, template));
            });
        });

        var bytes = document.GeneratePdf();
        
        if (!string.IsNullOrWhiteSpace(product.ExposureScenarioPdfPath) && System.IO.File.Exists(product.ExposureScenarioPdfPath))
        {
            _progress.Report("Anexando Cenários de Exposição (Merge PDF)...", 90);
            try
            {
                using var sourceStream = new System.IO.MemoryStream(bytes);
                using var destStream = new System.IO.MemoryStream();
                
                using var destPdf = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(destStream));
                var merger = new iText.Kernel.Utils.PdfMerger(destPdf);
                
                using var sourcePdf = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(sourceStream));
                merger.Merge(sourcePdf, 1, sourcePdf.GetNumberOfPages());
                
                using var annexPdf = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(product.ExposureScenarioPdfPath));
                merger.Merge(annexPdf, 1, annexPdf.GetNumberOfPages());
                
                sourcePdf.Close();
                annexPdf.Close();
                destPdf.Close();
                
                bytes = destStream.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to merge PDF: {ex.Message}");
            }
        }

        _progress.Report("SDS QuestPDF gerado com sucesso.", 100);
        return bytes;
    }

    private void RenderSdsSection(ColumnDescriptor inner, TemplateSectionSchema sec, TemplateBuilderSchema schema, Product product, List<SubstanceHazard> uniqueHazards, HashSet<string> hazardCodes, List<ProductComposition> compositions, Dictionary<string, string> liquidProps, List<string> hazardPhrases, List<ClpPhrase> pPhrases)
    {
        if (sec.Type == "RichText" || sec.Type == "SdsSectionText")
        {
            float scale = schema.Layout.SectionBaseScale > 0 ? (float)schema.Layout.SectionBaseScale : 1.0f;
            float baseFontSize = sec.FontSize > 0 ? (float)sec.FontSize : (float)schema.Theme.BaseFontSize * scale;
            if (!string.IsNullOrWhiteSpace(sec.Title))
            {
                inner.Item().PaddingBottom(6).Element(c => DrawSectionTitle(c, sec.Title, schema.Theme.SecondaryColor, baseFontSize));
            }
            var text = System.Text.RegularExpressions.Regex.Replace(sec.Content ?? "", "<[^>]*>", string.Empty);
            text = ReplacePlaceholders(text, product, null);
            inner.Item().Text(text).FontSize(baseFontSize);
            return;
        }

        string sectionKey = !string.IsNullOrWhiteSpace(sec.DataSource) && sec.DataSource.StartsWith("SdsSection") 
            ? sec.DataSource 
            : sec.Type;

        switch (sectionKey)
        {
            case "SdsSection1": ComposeSdsSection1(inner, product, schema, sec.Title); break;
            case "SdsSection2": ComposeSdsSection2(inner, uniqueHazards, hazardCodes, pPhrases, schema, sec.Title); break;
            case "SdsSection3": ComposeSdsSection3(inner, compositions, schema, sec.Title); break;
            case "SdsSection4": ComposeSdsSection4(inner, hazardCodes, product.SdsSection4CustomText, schema, sec.Title); break;
            case "SdsSection5": ComposeSdsSection5(inner, hazardCodes, schema, sec.Title); break;
            case "SdsSection6": ComposeSdsSection6(inner, schema, sec.Title); break;
            case "SdsSection7": ComposeSdsSection7(inner, hazardCodes, product.SdsSection7CustomText, schema, sec.Title); break;
            case "SdsSection8": ComposeSdsSection8(inner, schema, sec.Title); break;
            case "SdsSection9": ComposeSdsSection9(inner, liquidProps, schema, sec.Title); break;
            case "SdsSection10": ComposeSdsSection10(inner, schema, sec.Title); break;
            case "SdsSection11": ComposeSdsSection11(inner, hazardCodes, schema, sec.Title); break;
            case "SdsSection12": ComposeSdsSection12(inner, hazardCodes, schema, sec.Title); break;
            case "SdsSection13": ComposeSdsSection13(inner, schema, sec.Title); break;
            case "SdsSection14": ComposeSdsSection14(inner, hazardCodes, schema, sec.Title); break;
            case "SdsSection15": ComposeSdsSection15(inner, schema, sec.Title); break;
            case "SdsSection16": ComposeSdsSection16(inner, hazardPhrases, schema, sec.Title); break;
        }
    }

    private void DrawSdsTitle(ColumnDescriptor col, string title, TemplateBuilderSchema schema)
    {
        col.Item().PaddingTop(6).Element(c => DrawSectionTitle(c, title, schema.Theme.SecondaryColor, (float)schema.Theme.BaseFontSize));
    }

    private void ComposeSdsSection1(ColumnDescriptor col, Product product, TemplateBuilderSchema schema, string title = "SECTION 1: Identification of the substance/mixture and of the company/undertaking")
    {
        var companyInfo = _companyInfoOptions.CurrentValue;
        
        DrawSdsTitle(col, title, schema);
        col.Item().Text("1.1 Product identifier").Bold();
        col.Item().PaddingLeft(10).Text($"Product name: {product.Name}");
        col.Item().PaddingTop(4).Text("1.2 Relevant identified uses of the substance or mixture and uses advised against").Bold();
        col.Item().PaddingLeft(10).Text("Use of the Substance/Mixture: Industrial and research");
        col.Item().PaddingTop(4).Text("1.3 Details of the supplier of the safety data sheet").Bold();
        col.Item().PaddingLeft(10).Text($"Company: {companyInfo.Name}\nAddress: {companyInfo.Address}\nTelephone: {companyInfo.Telephone}\nEmail: {companyInfo.Email}");
        col.Item().PaddingTop(4).Text("1.4 Emergency telephone number").Bold();
        col.Item().PaddingLeft(10).Text(!string.IsNullOrWhiteSpace(companyInfo.EmergencyTelephone) ? companyInfo.EmergencyTelephone : (companyInfo.Telephone ?? ""));
    }

    private void ComposeSdsSection2(ColumnDescriptor col, List<SubstanceHazard> uniqueHazards, HashSet<string> hazardCodes, List<ClpPhrase> pPhrases, TemplateBuilderSchema schema, string title = "SECTION 2: Hazards identification")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("2.1 Classification of the substance or mixture").Bold();
        col.Item().Text("Classification (REGULATION (EC) No 1272/2008)");
        
        if (uniqueHazards.Any())
        {
            foreach (var h in uniqueHazards)
            {
                col.Item().PaddingLeft(10).Text($"{h.PhraseCode}: {h.Phrase?.Description}");
            }
        }
        else
        {
            col.Item().PaddingLeft(10).Text("Not classified as a hazardous mixture.");
        }

        col.Item().PaddingTop(4).Text("2.2 Label elements").Bold();
        
        var pictograms = new List<string>();
        if (hazardCodes.Any(h => h.StartsWith("H315") || h.StartsWith("H319") || h.StartsWith("H335"))) pictograms.Add("GHS07 (Exclamation mark)");
        if (hazardCodes.Any(h => h.StartsWith("H318") || h.StartsWith("H314"))) pictograms.Add("GHS05 (Corrosion)");
        if (hazardCodes.Any(h => h.StartsWith("H225") || h.StartsWith("H226"))) pictograms.Add("GHS02 (Flame)");
        if (hazardCodes.Any(h => h.StartsWith("H410") || h.StartsWith("H411"))) pictograms.Add("GHS09 (Environment)");
        if (hazardCodes.Any(h => h.StartsWith("H301") || h.StartsWith("H311") || h.StartsWith("H331"))) pictograms.Add("GHS06 (Skull and crossbones)");
        if (hazardCodes.Any(h => h.StartsWith("H350") || h.StartsWith("H340") || h.StartsWith("H360"))) pictograms.Add("GHS08 (Health hazard)");

        if (pictograms.Any())
        {
            col.Item().PaddingLeft(10).PaddingBottom(5).Text($"Warning Pictograms: {string.Join(", ", pictograms)}");
        }

        if (pPhrases != null && pPhrases.Any())
        {
            col.Item().PaddingTop(5).Text("Precautionary statements:").Bold();
            foreach (var p in pPhrases)
            {
                col.Item().PaddingLeft(10).Text($"{p.Code}: {p.Description}");
            }
        }

        if (pictograms.Any())
        {
            col.Item().PaddingLeft(10).Row(row =>
            {
                row.Spacing(10);
                foreach (var p in pictograms)
                {
                    var code = p.Split(' ')[0];
                    var imgPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Pictograms", $"{code}.png");
                    if (File.Exists(imgPath))
                    {
                        row.AutoItem().Height(40).Image(imgPath).FitArea();
                    }
                }
            });
        }
        else
        {
            col.Item().PaddingLeft(10).Text("No label elements required.");
        }
    }

    private void ComposeSdsSection3(ColumnDescriptor col, List<ProductComposition> compositions, TemplateBuilderSchema schema, string title = "SECTION 3: Composition/information on ingredients")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("3.2 Mixtures").Bold();
        col.Item().Text("Dangerous Components:");
        
        col.Item().PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten3).Padding(4).Text("Identifiers").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten3).Padding(4).Text("Chemical Name").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten3).Padding(4).Text("Classification (1272/2008)").Bold();
                header.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten3).Padding(4).Text("% Weight").Bold();
            });

            foreach (var comp in compositions)
            {
                if (comp.EchaSubstance != null)
                {
                    var substance = comp.EchaSubstance;
                    var subHazards = string.Join("; ", substance.Hazards?.Select(h => h.PhraseCode) ?? Array.Empty<string>());
                    
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"CAS: {substance.CasNumber}\nEC: {substance.EcNumber}");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(substance.ChemicalName);
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(subHazards);
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{comp.Percentage}%");
                }

                if (comp.SupplierProduct?.Compositions != null && comp.SupplierProduct.Compositions.Any())
                {
                    foreach (var inner in comp.SupplierProduct.Compositions)
                    {
                        var substance = inner.EchaSubstance;
                        if (substance == null) continue;
                        
                        var effectivePercent = Math.Round((comp.Percentage * inner.Percentage) / 100.0, 2);
                        var subHazards = string.Join("; ", substance.Hazards?.Select(h => h.PhraseCode) ?? Array.Empty<string>());
                        
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"CAS: {substance.CasNumber}\nEC: {substance.EcNumber}");
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(substance.ChemicalName);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(subHazards);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{effectivePercent}%");
                    }
                }
            }
        });
    }

    private void ComposeSdsSection4(ColumnDescriptor col, HashSet<string> hazardCodes, string customText, TemplateBuilderSchema schema, string title = "SECTION 4: First aid measures")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("4.1 Description of first aid measures").Bold();
        
        var general = "General advice: Consult a physician. Show this safety data sheet to the doctor in attendance.";
        var inhalation = hazardCodes.Any(h => h.StartsWith("H33")) 
            ? "If inhaled: If breathed in, move person into fresh air. If not breathing, give artificial respiration. Consult a physician."
            : "If inhaled: Remove to fresh air.";
            
        var skin = hazardCodes.Any(h => h.StartsWith("H31"))
            ? "In case of skin contact: Wash off with soap and plenty of water. Consult a physician if irritation develops."
            : "In case of skin contact: Wash off with soap and plenty of water.";
            
        var eye = hazardCodes.Any(h => h == "H319" || h == "H318")
            ? "In case of eye contact: Rinse thoroughly with plenty of water for at least 15 minutes and consult a physician."
            : "In case of eye contact: Flush eyes with water as a precaution.";
            
        var ingestion = hazardCodes.Any(h => h.StartsWith("H30"))
            ? "If swallowed: Never give anything by mouth to an unconscious person. Rinse mouth with water. Consult a physician."
            : "If swallowed: Rinse mouth with water.";

        col.Item().PaddingLeft(10).Text($"{general}\n{inhalation}\n{skin}\n{eye}\n{ingestion}");
    }

    private void ComposeSdsSection5(ColumnDescriptor col, HashSet<string> hazardCodes, TemplateBuilderSchema schema, string title = "SECTION 5: Firefighting measures")
    {
        DrawSdsTitle(col, title, schema);
        var isFlammable = hazardCodes.Any(h => h.StartsWith("H22"));
        
        col.Item().Text("5.1 Extinguishing media").Bold();
        col.Item().PaddingLeft(10).Text(isFlammable 
            ? "Suitable extinguishing media: Use water spray, alcohol-resistant foam, dry chemical or carbon dioxide." 
            : "Suitable extinguishing media: Use extinguishing measures that are appropriate to local circumstances and the surrounding environment.");
            
        col.Item().PaddingTop(4).Text("5.2 Special hazards arising from the substance or mixture").Bold();
        col.Item().PaddingLeft(10).Text("Carbon oxides, Nitrogen oxides (NOx).");
        
        col.Item().PaddingTop(4).Text("5.3 Advice for firefighters").Bold();
        col.Item().PaddingLeft(10).Text("Wear self-contained breathing apparatus for firefighting if necessary.");
    }

    private void ComposeSdsSection6(ColumnDescriptor col, TemplateBuilderSchema schema, string title = "SECTION 6: Accidental release measures")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("6.1 Personal precautions, protective equipment and emergency procedures").Bold();
        col.Item().PaddingLeft(10).Text("Use personal protective equipment. Avoid breathing vapours, mist or gas. Ensure adequate ventilation.");
        
        col.Item().PaddingTop(4).Text("6.2 Environmental precautions").Bold();
        col.Item().PaddingLeft(10).Text("Prevent further leakage or spillage if safe to do so. Do not let product enter drains.");
        
        col.Item().PaddingTop(4).Text("6.3 Methods and materials for containment and cleaning up").Bold();
        col.Item().PaddingLeft(10).Text("Soak up with inert absorbent material and dispose of as hazardous waste. Keep in suitable, closed containers for disposal.");
    }

    private void ComposeSdsSection7(ColumnDescriptor col, HashSet<string> hazardCodes, string customText, TemplateBuilderSchema schema, string title = "SECTION 7: Handling and storage")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("7.1 Precautions for safe handling").Bold();
        col.Item().PaddingLeft(10).Text("Avoid contact with skin and eyes. Avoid inhalation of vapour or mist.");
        
        if (hazardCodes.Any(h => h.StartsWith("H22")))
        {
            col.Item().PaddingLeft(10).Text("Keep away from sources of ignition - No smoking. Take measures to prevent the build up of electrostatic charge.");
        }
        
        col.Item().PaddingTop(4).Text("7.2 Conditions for safe storage, including any incompatibilities").Bold();
        col.Item().PaddingLeft(10).Text("Store in cool place. Keep container tightly closed in a dry and well-ventilated place.");
    }

    private void ComposeSdsSection8(ColumnDescriptor col, TemplateBuilderSchema schema, string title = "SECTION 8: Exposure controls/personal protection")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("8.1 Control parameters").Bold();
        col.Item().PaddingLeft(10).Text("Components with workplace control parameters: No data available.");
        
        col.Item().PaddingTop(4).Text("8.2 Exposure controls").Bold();
        col.Item().PaddingLeft(10).Text("Appropriate engineering controls: Handle in accordance with good industrial hygiene and safety practice.\nPersonal protective equipment: Safety glasses with side-shields, chemical resistant gloves, protective clothing.");
    }

    private void ComposeSdsSection9(ColumnDescriptor col, Dictionary<string, string> liquidProps, TemplateBuilderSchema schema, string title = "SECTION 9: Physical and chemical properties")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("9.1 Information on basic physical and chemical properties").Bold();
        
        var state = liquidProps.TryGetValue("Physical form", out var phys) ? phys : "Liquid";
        var color = liquidProps.TryGetValue("Appearance", out var app) ? app : "Not specified";
        var visco = liquidProps.TryGetValue("Viscosity", out var vis) ? vis : "No data available";
        
        col.Item().PaddingLeft(10).Text($"Physical state: {state}");
        col.Item().PaddingLeft(10).Text($"Colour: {color}");
        col.Item().PaddingLeft(10).Text($"Viscosity: {visco}");
        col.Item().PaddingLeft(10).Text("Odour: Characteristic");
        col.Item().PaddingLeft(10).Text("Flash point: No data available");
    }

    private void ComposeSdsSection10(ColumnDescriptor col, TemplateBuilderSchema schema, string title = "SECTION 10: Stability and reactivity")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("10.1 Reactivity: No data available");
        col.Item().Text("10.2 Chemical stability: Stable under recommended storage conditions.");
        col.Item().Text("10.3 Possibility of hazardous reactions: No data available");
        col.Item().Text("10.4 Conditions to avoid: Heat, flames and sparks (if flammable).");
        col.Item().Text("10.5 Incompatible materials: Strong oxidizing agents.");
    }

    private void ComposeSdsSection11(ColumnDescriptor col, HashSet<string> hazardCodes, TemplateBuilderSchema schema, string title = "SECTION 11: Toxicological information")
    {
        DrawSdsTitle(col, title, schema);
        var isToxic = hazardCodes.Any(h => h.StartsWith("H30") || h.StartsWith("H31") || h.StartsWith("H33"));
        col.Item().Text("11.1 Information on hazard classes as defined in Regulation (EC) No 1272/2008").Bold();
        col.Item().PaddingLeft(10).Text(isToxic ? "Acute toxicity: Classified based on available ingredients data." : "Acute toxicity: Based on available data, the classification criteria are not met.");
        col.Item().PaddingLeft(10).Text("Skin corrosion/irritation: Refer to Section 2.");
        col.Item().PaddingLeft(10).Text("Serious eye damage/eye irritation: Refer to Section 2.");
    }

    private void ComposeSdsSection12(ColumnDescriptor col, HashSet<string> hazardCodes, TemplateBuilderSchema schema, string title = "SECTION 12: Ecological information")
    {
        DrawSdsTitle(col, title, schema);
        var isEcoToxic = hazardCodes.Any(h => h.StartsWith("H4"));
        col.Item().Text("12.1 Toxicity: " + (isEcoToxic ? "Toxic to aquatic life." : "No data available."));
        col.Item().Text("12.2 Persistence and degradability: No data available.");
        col.Item().Text("12.3 Bioaccumulative potential: No data available.");
    }

    private void ComposeSdsSection13(ColumnDescriptor col, TemplateBuilderSchema schema, string title = "SECTION 13: Disposal considerations")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("13.1 Waste treatment methods").Bold();
        col.Item().PaddingLeft(10).Text("Product: Offer surplus and non-recyclable solutions to a licensed disposal company.\nContaminated packaging: Dispose of as unused product.");
    }

    private void ComposeSdsSection14(ColumnDescriptor col, HashSet<string> hazardCodes, TemplateBuilderSchema schema, string title = "SECTION 14: Transport information")
    {
        DrawSdsTitle(col, title, schema);
        
        var adrClasses = new List<string>();
        if (hazardCodes.Any(h => h.StartsWith("H22"))) adrClasses.Add("ADR3 (Flammable Liquid)");
        if (hazardCodes.Any(h => h.StartsWith("H314"))) adrClasses.Add("ADR8 (Corrosive)");
        if (hazardCodes.Any(h => h.StartsWith("H301") || h.StartsWith("H311") || h.StartsWith("H331"))) adrClasses.Add("ADR6 (Toxic)");
        if (hazardCodes.Any(h => h.StartsWith("H400") || h.StartsWith("H410") || h.StartsWith("H411"))) adrClasses.Add("ADR9 (Environmentally Hazardous)");

        if (adrClasses.Any())
        {
            col.Item().PaddingLeft(10).PaddingBottom(5).Text($"Regulated under ADR/RID, IMDG, IATA. Hazard Classes: {string.Join(", ", adrClasses)}");
            
            col.Item().PaddingLeft(10).Row(row =>
            {
                row.Spacing(10);
                foreach (var adr in adrClasses)
                {
                    var code = adr.Split(' ')[0]; // gets "ADR3", "ADR8", etc.
                    var imgPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Pictograms", $"{code}.png");
                    if (File.Exists(imgPath))
                    {
                        row.AutoItem().Height(40).Image(imgPath).FitArea();
                    }
                }
            });
        }
        else
        {
            col.Item().PaddingLeft(10).Text("Not regulated as a dangerous good under ADR/RID, IMDG, or IATA unless specifically classified in Section 2.");
        }
    }

    private void ComposeSdsSection15(ColumnDescriptor col, TemplateBuilderSchema schema, string title = "SECTION 15: Regulatory information")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("15.1 Safety, health and environmental regulations/legislation specific for the substance or mixture").Bold();
        col.Item().PaddingLeft(10).Text("This safety datasheet complies with the requirements of Regulation (EC) No. 1907/2006.");
    }

    private void ComposeSdsSection16(ColumnDescriptor col, List<string> hazardPhrases, TemplateBuilderSchema schema, string title = "SECTION 16: Other information")
    {
        DrawSdsTitle(col, title, schema);
        col.Item().Text("Full text of H-Statements referred to under sections 2 and 3:").Bold();
        if (hazardPhrases.Any())
        {
            foreach (var phrase in hazardPhrases)
            {
                col.Item().PaddingLeft(10).Text(phrase);
            }
        }
        else
        {
            col.Item().PaddingLeft(10).Text("Not applicable.");
        }
        
        col.Item().PaddingTop(10).Text("The information contained herein is based on the present state of our knowledge and does not therefore guarantee certain properties. Recipients of our product must take responsibility for observing existing laws and regulations.").FontSize((float)schema.Theme.BaseFontSize - 1f).FontColor(Colors.Grey.Medium);
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







