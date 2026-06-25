namespace NanopaintHub.Infrastructure.Documents.DependencyInjection;

public class PdfGeneratorOptions
{
    public string TdsEngine { get; set; } = "QuestPDF"; // Options: "WebView2", "QuestPDF", "iText7"
    public string SdsEngine { get; set; } = "QuestPDF"; // Options: "WebView2", "QuestPDF", "iText7"
}
