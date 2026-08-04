using Xunit;
using NanopaintHub.Application.Tds;

namespace NanopaintHub.UnitTests.Tds;

public class TdsHtmlGeneratorTests
{
    [Fact]
    public void GenerateHtml_ShouldProduceCorrectStructure()
    {
        // Arrange
        var generator = new TdsHtmlGenerator();
        var schema = new TemplateBuilderSchema();

        schema.Footer.LegalText = "This is a legal footer disclaimer.";
        
        schema.Sections.Add(new TemplateSectionSchema
        {
            Title = "Section A",
            Type = "RichText",
            Content = "<p>Rich Content A</p>"
        });

        // Act
        var html = generator.GenerateHtml(schema);

        // Assert
        Assert.Contains("{{Category}}", html);
        Assert.Contains("Technical Data Sheet", html);
        Assert.Contains("{{Name}}", html);
        Assert.Contains("This is a legal footer disclaimer.", html);
        Assert.Contains("Section A", html);
        Assert.Contains("<p>Rich Content A</p>", html);
    }

    [Fact]
    public void GenerateCss_ShouldIncludeCustomThemeVariables()
    {
        // Arrange
        var generator = new TdsHtmlGenerator();
        var schema = new TemplateBuilderSchema();
        schema.Theme.PrimaryColor = "#aabbcc";
        schema.Theme.SecondaryColor = "#ddeeff";
        schema.Theme.FontFamily = "Outfit";
        schema.Layout.TopMargin = 50;
        schema.Layout.BottomMargin = 45;

        // Act
        var css = generator.GenerateCss(schema);

        // Assert
        Assert.Contains("--primary: #aabbcc;", css);
        Assert.Contains("--secondary: #ddeeff;", css);
        Assert.Contains("font-family: \"Outfit\"", css);
        Assert.Contains("--margin-top: 50mm;", css);
        Assert.Contains("--margin-bottom: 45mm;", css);
    }
}
