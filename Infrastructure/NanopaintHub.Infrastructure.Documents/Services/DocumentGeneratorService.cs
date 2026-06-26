namespace NanopaintHub.Infrastructure.Documents.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

public sealed class DocumentGeneratorService : IHtmlEngineService, IDocumentGeneratorService
{
    private readonly IPdfGenerationProgressNotifier _progress;
    private readonly IHtmlToPdfGenerator _pdfGenerator;
    private readonly TdsDataGatherer _tdsDataGatherer;
    private readonly SdsDataGatherer _sdsDataGatherer;

    public DocumentGeneratorService(
        IPdfGenerationProgressNotifier progress,
        IHtmlToPdfGenerator pdfGenerator,
        TdsDataGatherer tdsDataGatherer,
        SdsDataGatherer sdsDataGatherer)
    {
        _progress = progress;
        _pdfGenerator = pdfGenerator;
        _tdsDataGatherer = tdsDataGatherer;
        _sdsDataGatherer = sdsDataGatherer;
    }

    public async Task<byte[]> GenerateTdsAsync(Product product, DocumentTemplate template, TestBatch? testBatch = null, PdfGenerationSettings? settings = null)
    {
        var html = await GenerateHtmlAsync(product, template, false, testBatch);
        return await GeneratePdfFromHtmlAsync(html, template, settings);
    }

    public async Task<byte[]> GenerateSdsAsync(Product product, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        var html = await GenerateHtmlAsync(product, template, true, null);
        return await GeneratePdfFromHtmlAsync(html, template, settings);
    }

    public async Task<string> GenerateRawBodyHtmlAsync(Product product, DocumentTemplate template, bool isSds = false, TestBatch? testBatch = null)
    {
        if (isSds)
        {
            return await _sdsDataGatherer.GatherHtmlAsync(product, template);
        }
        else
        {
            return await _tdsDataGatherer.GatherHtmlAsync(product, template, testBatch);
        }
    }

    public async Task<string> GenerateHtmlAsync(Product product, DocumentTemplate template, bool isSds = false, TestBatch? testBatch = null)
    {
        var kind = isSds ? "SDS" : "TDS";
        Report($"Iniciando pipeline {kind}...", 5);

        string innerHtml = await GenerateRawBodyHtmlAsync(product, template, isSds, testBatch);

        Report("HTML convertido e pronto para renderizacao.", 60);

        return WrapHtmlWithAssets(innerHtml, template);
    }

    private string WrapHtmlWithAssets(string innerHtml, DocumentTemplate template)
    {
        // Inject CSS from the template if present
        if (!string.IsNullOrWhiteSpace(template.CssStructure))
        {
            var styleTag = $"<style>\n{template.CssStructure}\n</style>";
            if (innerHtml.Contains("</head>", StringComparison.OrdinalIgnoreCase))
            {
                innerHtml = innerHtml.Replace("</head>", styleTag + "\n</head>", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                innerHtml = $"<head>\n{styleTag}\n</head>\n" + innerHtml;
            }
        }

        // Inject Paged.js script offline
        var pagedJsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "paged.polyfill.js");
        var pagedJsScript = "<script src=\"https://unpkg.com/pagedjs/dist/paged.polyfill.js\"></script>";
        if (File.Exists(pagedJsPath))
        {
            var scriptContent = File.ReadAllText(pagedJsPath);
            pagedJsScript = $"<script>\n{scriptContent}\n</script>";
        }

        if (innerHtml.Contains("</head>", StringComparison.OrdinalIgnoreCase))
        {
            innerHtml = innerHtml.Replace("</head>", pagedJsScript + "\n</head>", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            innerHtml = $"<head>\n{pagedJsScript}\n</head>\n" + innerHtml;
        }

        var finalHtml = ApplyLetterheadBackground(innerHtml, template.LetterheadPdfPath);

        // Convert local images to Base64 to bypass about:blank CORS restrictions
        var imgRegex = new System.Text.RegularExpressions.Regex(@"<img[^>]+src=[""'](file:///[^""']+|[a-zA-Z]:\\[^""']+|/[^""']+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        finalHtml = imgRegex.Replace(finalHtml, match => 
        {
            var src = match.Groups[1].Value;
            var localPath = src.StartsWith("file:///") ? src.Substring(8).Replace("/", "\\") : src;
            if (File.Exists(localPath))
            {
                var bytes = File.ReadAllBytes(localPath);
                var ext = Path.GetExtension(localPath).ToLowerInvariant();
                var mimeType = ext switch { ".png" => "image/png", ".jpg" => "image/jpeg", ".jpeg" => "image/jpeg", ".gif" => "image/gif", ".svg" => "image/svg+xml", ".webp" => "image/webp", _ => "application/octet-stream" };
                var base64 = Convert.ToBase64String(bytes);
                return match.Value.Replace(src, $"data:{mimeType};base64,{base64}");
            }
            return match.Value;
        });

        return finalHtml;
    }

    public async Task<byte[]> GeneratePdfFromRawBodyAsync(string bodyHtml, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        var finalHtml = WrapHtmlWithAssets(bodyHtml, template);
        return await GeneratePdfFromHtmlAsync(finalHtml, template, settings);
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string html, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        Report("A iniciar motor PDF WinUI...", 80);
        
        html = html.Replace("<!-- pagebreak -->", "<div style=\"page-break-after: always;\"></div>");

        var pageFormat = (settings?.PageFormat?.ToLowerInvariant() == "letter") ? "Letter" : "A4";
        var isLandscape = (settings?.PageOrientation?.ToLowerInvariant() == "landscape");

        Report("A gerar ficheiro PDF...", 95);
        var data = await _pdfGenerator.GeneratePdfAsync(html, pageFormat, isLandscape);
        Report("PDF gerado com sucesso.", 100);
        return data;
    }

    private static string ApplyLetterheadBackground(string html, string? letterheadPath)
    {
        var resolved = ResolveLetterheadPath(letterheadPath);
        if (resolved is null || !IsRasterImage(resolved)) return html;

        var imageUri = new Uri(resolved).AbsoluteUri;
        var style = $"<style>@page{{size:A4;margin:0;}}html,body{{margin:0;min-height:297mm;background-image:url('{imageUri}');background-size:cover;background-repeat:no-repeat;background-position:top center;}}</style>";

        if (html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
            return html.Replace("</head>", style + "</head>", StringComparison.OrdinalIgnoreCase);

        return style + html;
    }

    private static string? ResolveLetterheadPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        path = path.Trim();
        if (File.Exists(path)) return path;
        var underApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub", path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return File.Exists(underApp) ? underApp : null;
    }

    private static bool IsRasterImage(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif";
    }

    private void Report(string message, double? percent = null)
    {
        Console.WriteLine($"[DOC][PROGRESS] {(percent.HasValue ? $"{percent:0}% - " : string.Empty)}{message}");
        _progress.Report(message, percent);
    }
}
