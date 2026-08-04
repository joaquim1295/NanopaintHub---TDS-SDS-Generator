using Xunit;
using System.Text.Json;
using NanopaintHub.Application.Tds;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.Documents.Services;

namespace NanopaintHub.UnitTests.Tds;

public class TdsLayoutTests
{
    [Fact]
    public void LayoutSettings_SerializeAndDeserializeCorrectly()
    {
        // Arrange
        var schema = new TemplateBuilderSchema
        {
            Layout = new TemplateLayoutSchema
            {
                TopMargin = 85,
                BottomMargin = 75,
                LeftMargin = 30,
                RightMargin = 30,
                HeaderPaddingTop = 15,
                HeaderPaddingBottom = 12,
                FooterPaddingTop = 10,
                FooterPaddingBottom = 15,
                SectionMarginBottom = 20
            }
        };

        schema.Sections.Add(new TemplateSectionSchema
        {
            Id = "sec1",
            Title = "Section 1",
            PageBreakBefore = true,
            FontSize = 12,
            MarginBottom = 25,
            MinHeight = 150
        });

        // Act
        var json = JsonSerializer.Serialize(schema);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserialized = JsonSerializer.Deserialize<TemplateBuilderSchema>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Layout);
        Assert.Equal(85, deserialized.Layout.TopMargin);
        Assert.Equal(75, deserialized.Layout.BottomMargin);
        Assert.Equal(30, deserialized.Layout.LeftMargin);
        Assert.Equal(30, deserialized.Layout.RightMargin);
        Assert.Equal(15, deserialized.Layout.HeaderPaddingTop);
        Assert.Equal(12, deserialized.Layout.HeaderPaddingBottom);
        Assert.Equal(10, deserialized.Layout.FooterPaddingTop);
        Assert.Equal(15, deserialized.Layout.FooterPaddingBottom);
        Assert.Equal(20, deserialized.Layout.SectionMarginBottom);

        Assert.Single(deserialized.Sections);
        Assert.True(deserialized.Sections[0].PageBreakBefore);
        Assert.Equal(12, deserialized.Sections[0].FontSize);
        Assert.Equal(25, deserialized.Sections[0].MarginBottom);
        Assert.Equal(150, deserialized.Sections[0].MinHeight);
    }

    [Fact]
    public void AdvancedDocumentRenderer_GeneratesCustomCssVariables()
    {
        // Arrange
        var schema = new TemplateBuilderSchema
        {
            Layout = new TemplateLayoutSchema
            {
                TopMargin = 55,
                BottomMargin = 45,
                LeftMargin = 25,
                RightMargin = 25,
                HeaderPaddingTop = 18,
                HeaderPaddingBottom = 14,
                FooterPaddingTop = 12,
                FooterPaddingBottom = 18,
                SectionMarginBottom = 22
            }
        };

        schema.Sections.Add(new TemplateSectionSchema
        {
            Id = "sec1",
            Title = "Section 1",
            PageBreakBefore = true,
            FontSize = 14,
            MarginBottom = 30,
            MinHeight = 200
        });

        var templateData = new {
            HasBatch = false,
            Charts = new { HasLinearity = false }
        };

        // Act
        var html = AdvancedDocumentRenderer.Render(schema, templateData);

        // Assert
        Assert.Contains("--margin-top: 55mm;", html);
        Assert.Contains("--margin-bottom: 45mm;", html);
        Assert.Contains("--margin-left: 25mm;", html);
        Assert.Contains("--margin-right: 25mm;", html);
        Assert.Contains("--section-margin-bottom: 22px;", html);

        // Verify page break element is generated
        Assert.Contains("class=\"page-break-preview\"", html);
        Assert.Contains("break-before: page;", html);

        // Verify section styles are generated
        Assert.Contains("font-size: 14pt;", html);
        Assert.Contains("margin-bottom: 30px;", html);
        Assert.Contains("min-height: 200px;", html);


    }

    [Fact]
    public void TemplateHtmlParser_ParsesLayoutAndSectionStylesFromHtmlCss()
    {
        // Arrange
        var css = @"
:root {
  --margin-top: 60px;
  --margin-bottom: 50px;
  --margin-left: 25px;
  --margin-right: 25px;
  --header-padding-top: 15mm;
  --header-padding-bottom: 11mm;
  --footer-padding-top: 10px;
  --footer-padding-bottom: 15px;
  --section-margin-bottom: 16px;
}
";
        var html = @"
<div class=""page"">
  <div class=""page-break-preview""><span>--- QUEBRA DE PÁGINA ---</span></div>
  <div style=""page-break-before: always; break-before: page;""></div>
  <section class=""section full-width"" style=""font-family: 'Open Sans', sans-serif; color: #333333; font-size: 11pt; margin-bottom: 25px; min-height: 180px;"">
    <h2>Section 1</h2>
    <div class=""content"">Hello</div>
  </section>
  <section class=""section"" style=""font-family: Arial; color: red; font-size: 10pt;"">
    <h2>Section 2</h2>
  </section>
</div>
";

        // Act
        var parsedSchema = TemplateHtmlParser.ParseToSchema(html, css);

        // Assert
        Assert.NotNull(parsedSchema);
        Assert.NotNull(parsedSchema.Layout);
        Assert.Equal(60, parsedSchema.Layout.TopMargin);
        Assert.Equal(50, parsedSchema.Layout.BottomMargin);
        Assert.Equal(25, parsedSchema.Layout.LeftMargin);
        Assert.Equal(25, parsedSchema.Layout.RightMargin);
        Assert.Equal(15, parsedSchema.Layout.HeaderPaddingTop);
        Assert.Equal(11, parsedSchema.Layout.HeaderPaddingBottom);
        Assert.Equal(10, parsedSchema.Layout.FooterPaddingTop);
        Assert.Equal(15, parsedSchema.Layout.FooterPaddingBottom);
        Assert.Equal(16, parsedSchema.Layout.SectionMarginBottom);

        Assert.Equal(2, parsedSchema.Sections.Count);
        
        // Section 1
        Assert.True(parsedSchema.Sections[0].PageBreakBefore);
        Assert.Equal("Open Sans", parsedSchema.Sections[0].FontFamily);
        Assert.Equal("#333333", parsedSchema.Sections[0].TextColor);
        Assert.Equal(11, parsedSchema.Sections[0].FontSize);
        Assert.Equal(25, parsedSchema.Sections[0].MarginBottom);
        Assert.Equal(180, parsedSchema.Sections[0].MinHeight);

        // Section 2
        Assert.False(parsedSchema.Sections[1].PageBreakBefore);
        Assert.Equal("Arial", parsedSchema.Sections[1].FontFamily);
        Assert.Equal("red", parsedSchema.Sections[1].TextColor);
        Assert.Equal(10, parsedSchema.Sections[1].FontSize);
    }

    [Fact]
    public void AdvancedDocumentRenderer_GeneratesChartsEvenWithoutLinearity()
    {
        // Arrange
        var schema = new TemplateBuilderSchema();
        schema.Sections.Add(new TemplateSectionSchema
        {
            Id = "sec-charts",
            Title = "Charts",
            Type = "Charts"
        });

        var templateData = new
        {
            HasBatch = false,
            Charts = new
            {
                HasLinearity = false,
                HasResistance = true,
                ResistanceSvg = "<svg>Resistance</svg>"
            }
        };

        // Act
        var html = AdvancedDocumentRenderer.Render(schema, templateData);

        // Assert
        // The charts container must be present since Charts is not null
        Assert.Contains("class=\"chart-grid\"", html);
        Assert.Contains("<svg>Resistance</svg>", html);
        Assert.DoesNotContain("Linearity", html);
    }

    [Fact]
    public async Task TestSdsPdfLayoutGeneration()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        // Arrange
        var progress = new Moq.Mock<IPdfGenerationProgressNotifier>();
        var chartRenderer = new Moq.Mock<ITdsChartRenderer>();
        var companyInfo = new Moq.Mock<Microsoft.Extensions.Options.IOptionsMonitor<NanopaintHub.Domain.Common.CompanyInfoOptions>>();
        companyInfo.Setup(c => c.CurrentValue).Returns(new NanopaintHub.Domain.Common.CompanyInfoOptions
        {
            Name = "Nanopaint",
            Address = "Braga, Portugal",
            Telephone = "+351 123 456 789",
            Email = "info@nanopaint.com"
        });
        var phraseRepo = new Moq.Mock<IClpPhraseRepository>();
        phraseRepo.Setup(p => p.GetAllAsync()).Returns(Task.FromResult<IEnumerable<ClpPhrase>>(new List<ClpPhrase>()));

        var generator = new QuestPdfDocumentGeneratorService(
            progress.Object,
            new TdsDataMergeService(),
            chartRenderer.Object,
            companyInfo.Object,
            phraseRepo.Object);

        var product = new Product
        {
            Name = "Test Product",
            Code = "PRV-001",
            Description = "Test Product Description",
            DocumentVersion = 1,
            RevisionDate = DateTime.Now,
            LiquidPropertiesJson = "{}",
            FeaturesJson = "[]",
            Compositions = new List<ProductComposition>
            {
                new ProductComposition
                {
                    Percentage = 60,
                    EchaSubstance = new EchaSubstance
                    {
                        ChemicalName = "Ethanol",
                        CasNumber = "64-17-5",
                        EcNumber = "200-578-6"
                    }
                }
            }
        };

        var template = new DocumentTemplate
        {
            BuilderJson = "{}"
        };

        // Act
        var bytes = await generator.GenerateSdsAsync(product, template);

        // Assert
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        // Guardar o PDF gerado em disco para podermos abri-lo e verificar visualmente!
        Directory.CreateDirectory("Output");
        File.WriteAllBytes(Path.Combine("Output", "sds-spacing-test.pdf"), bytes);
    }
}
