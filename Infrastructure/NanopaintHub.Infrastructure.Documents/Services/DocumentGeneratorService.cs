namespace NanopaintHub.Infrastructure.Documents.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HandlebarsDotNet;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.Documents.Tds;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.Documents.Tds;

public sealed class DocumentGeneratorService : IDocumentGeneratorService
{
    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(90);
    private readonly ISupplierComponentRepository _supplierComponents;
    private readonly IPdfGenerationProgressNotifier _progress;
    private readonly TdsDataMergeService _tdsMerge;
    private readonly ITdsChartRenderer _chartRenderer;
    private readonly IPdfLayoutService _pdfLayout;
    private readonly IHtmlToPdfGenerator _pdfGenerator;

    public DocumentGeneratorService(
        ISupplierComponentRepository supplierComponents,
        IPdfGenerationProgressNotifier progress,
        TdsDataMergeService tdsMerge,
        ITdsChartRenderer chartRenderer,
        IPdfLayoutService pdfLayout,
        IHtmlToPdfGenerator pdfGenerator)
    {
        _supplierComponents = supplierComponents;
        _progress = progress;
        _tdsMerge = tdsMerge;
        _chartRenderer = chartRenderer;
        _pdfLayout = pdfLayout;
        _pdfGenerator = pdfGenerator;
    }

    public Task<byte[]> GenerateTdsAsync(Product product, DocumentTemplate template, TestBatch? testBatch = null, PdfGenerationSettings? settings = null) =>
        GeneratePdfAsync(product, template, isSds: false, testBatch, settings);

    public Task<byte[]> GenerateSdsAsync(Product product, DocumentTemplate template, PdfGenerationSettings? settings = null) =>
        GeneratePdfAsync(product, template, isSds: true, testBatch: null, settings);

    private async Task<byte[]> GeneratePdfAsync(Product product, DocumentTemplate template, bool isSds, TestBatch? testBatch, PdfGenerationSettings? settings)
    {
        var finalHtml = await GenerateHtmlAsync(product, template, isSds, testBatch);
        return await GeneratePdfFromHtmlAsync(finalHtml, template, settings);
    }

    public async Task<string> GenerateHtmlAsync(Product product, DocumentTemplate template, bool isSds, TestBatch? testBatch = null)
    {
        var kind = isSds ? "SDS" : "TDS";
        Console.WriteLine($"[DOC] {kind} pipeline start. ProductId={product.Id}, TemplateId={template.Id}");
        Report($"Iniciando pipeline {kind}...", 5);

        EnsureValidInputs(product, template, kind);
        Report("Validacao de entradas concluida.", 10);

        var featuresList = SafeDeserialize(product.FeaturesJson, new List<string>(), "FeaturesJson");
        var liquidProps = SafeDeserialize(product.LiquidPropertiesJson, new Dictionary<string, string>(), "LiquidPropertiesJson");
        var mechanicalProps = SafeDeserialize(product.MechanicalPropertiesJson, new Dictionary<string, string>(), "MechanicalPropertiesJson");
        var functionalProps = SafeDeserialize(product.FunctionalPropertiesJson, new Dictionary<string, string>(), "FunctionalPropertiesJson");
        var jsonExtendedSpecs = TdsSensorDataMerger.ParseProductSpecs(product.TdsSensorSpecsJson);

        Console.WriteLine($"[DOC] Handlebars compile started for {kind}.");
        Report("Compilando template Handlebars...", 20);
        
        Console.WriteLine($"[DOC] Handlebars compile finished for {kind}.");
        Report("Template Handlebars compilado.", 30);

        object templateData;
        if (isSds)
        {
            Console.WriteLine("[DOC] Loading supplier/components for SDS...");
            Report("A carregar componentes SDS...", 40);
            var rows = await _supplierComponents.GetAllWithSupplierAsync();
            var components = rows
                .Select(r => new
                {
                    r.SupplierName,
                    r.ComponentName,
                    r.CasNumber,
                    SdsPropertiesJson = r.SdsPropertiesJson,
                    SdsProperties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(r.SdsPropertiesJson) ??
                                    new Dictionary<string, JsonElement>()
                })
                .ToList();
            Console.WriteLine($"[DOC] Loaded {components.Count} components for SDS.");
            Report($"Componentes SDS carregados ({components.Count}).", 50);

            templateData = new
            {
                product.Name,
                product.Code,
                product.Description,
                RevisionDate = product.RevisionDate.ToString("dd/MM/yyyy"),
                Version = product.DocumentVersion,
                Features = featuresList,
                LiquidProperties = liquidProps,
                MechanicalProperties = mechanicalProps,
                FunctionalProperties = functionalProps,
                Components = components,
                DocumentKind = "SDS",
                Placeholders = jsonExtendedSpecs.PlaceholderValues,
                Assets = jsonExtendedSpecs.TemplateAssets
            };
        }
        else
        {
            var mergedSpecs = _tdsMerge.Merge(product, testBatch);
            var imageFallback = testBatch?.CurveGraphPath;
            var chartSvgs = _chartRenderer.RenderAll(
                mergedSpecs,
                linearityImagePath: imageFallback,
                resistanceImagePath: imageFallback,
                hysteresisImagePath: imageFallback);
            var batchNumber = _tdsMerge.ResolveBatchNumber(testBatch);

            var logoBase64 = ReadLogoBase64();
            templateData = TdsTemplateViewModelBuilder.Build(
                product.Name,
                product.Code,
                product.Description,
                product.RevisionDate.ToString("dd/MM/yyyy"),
                product.DocumentVersion,
                featuresList,
                liquidProps,
                mechanicalProps,
                functionalProps,
                mergedSpecs,
                chartSvgs,
                batchNumber,
                logoBase64);
        }

        string innerHtml;
        if (kind == "TDS")
        {
            TemplateBuilderSchema? schema = null;
            if (!string.IsNullOrWhiteSpace(template.BuilderJson))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                schema = JsonSerializer.Deserialize<TemplateBuilderSchema>(template.BuilderJson, options);
            }

            if (schema == null || schema.Sections == null || schema.Sections.Count == 0)
            {
                schema = TemplateHtmlParser.ParseToSchema(template.HtmlStructure, template.CssStructure);
            }

            _pdfLayout.NormalizeLayout(schema, template);

            innerHtml = AdvancedDocumentRenderer.Render(schema!, templateData);
        }
        else
        {
            var compiledTemplate = CompileTemplate(template.HtmlStructure);
            innerHtml = compiledTemplate(templateData);
        }

        if (string.IsNullOrWhiteSpace(innerHtml))
        {
            throw new InvalidOperationException($"Handlebars render devolveu HTML vazio para {kind}.");
        }

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
                // If there's no head, prepend it (or wrap it)
                innerHtml = $"<head>\n{styleTag}\n</head>\n" + innerHtml;
            }
        }

        // Always inject Paged.js script offline
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
        Console.WriteLine($"[DOC] HTML rendered for {kind}. Length={finalHtml.Length} chars.");
        Report("HTML convertido e pronto para renderizacao.", 60);

        return finalHtml;
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string html, DocumentTemplate template, PdfGenerationSettings? settings = null)
    {
        Report("A iniciar motor PDF WinUI...", 80);
        Report("A converter HTML em pagina renderizada...", 88);
        
        html = html.Replace("<!-- pagebreak -->", "<div style=\"page-break-after: always;\"></div>");

        var pageFormat = (settings?.PageFormat?.ToLowerInvariant() == "letter") ? "Letter" : "A4";
        var isLandscape = (settings?.PageOrientation?.ToLowerInvariant() == "landscape");

        Report("A gerar ficheiro PDF...", 95);
        var data = await _pdfGenerator.GeneratePdfAsync(html, pageFormat, isLandscape);
        Console.WriteLine($"[DOC] PDF generated successfully. Bytes={data.Length}");
        Report("PDF gerado com sucesso.", 100);
        return data;
    }

    /// <summary>
    /// Papel timbrado: ficheiros de imagem (PNG/JPEG/WebP/GIF) são injetados como fundo de página.
    /// Caminhos absolutos ou relativos à pasta %LocalAppData%\NanopaintHub\.
    /// PDF como letterhead fica para integração futura (merge).
    /// </summary>
    private static string ApplyLetterheadBackground(string html, string? letterheadPath)
    {
        var resolved = ResolveLetterheadPath(letterheadPath);
        if (resolved is null || !IsRasterImage(resolved))
        {
            return html;
        }

        var imageUri = new Uri(resolved).AbsoluteUri;
        var style =
            $"<style>@page{{size:A4;margin:0;}}html,body{{margin:0;min-height:297mm;background-image:url('{imageUri}');background-size:cover;background-repeat:no-repeat;background-position:top center;}}</style>";

        if (html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
        {
            return html.Replace("</head>", style + "</head>", StringComparison.OrdinalIgnoreCase);
        }

        return style + html;
    }

    private static string? ResolveLetterheadPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        path = path.Trim();
        if (File.Exists(path))
        {
            return path;
        }

        var underApp =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NanopaintHub",
                path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        return File.Exists(underApp) ? underApp : null;
    }

    private static bool IsRasterImage(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif";
    }

    private static void EnsureValidInputs(Product product, DocumentTemplate template, string kind)
    {
        if (string.IsNullOrWhiteSpace(product.Name) || string.IsNullOrWhiteSpace(product.Code))
        {
            throw new InvalidOperationException($"Produto inválido para geração de {kind}. Nome e Código são obrigatórios.");
        }

        if (string.IsNullOrWhiteSpace(template.TemplateName))
        {
            throw new InvalidOperationException($"Template inválido para {kind}: nome em falta.");
        }

        if (string.IsNullOrWhiteSpace(template.HtmlStructure))
        {
            throw new InvalidOperationException($"Template inválido para {kind}: HTML vazio.");
        }
    }

    private static HandlebarsTemplate<object, object> CompileTemplate(string htmlStructure)
    {
        try
        {
            return Handlebars.Compile(htmlStructure);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Falha ao compilar template Handlebars: {ex.Message}", ex);
        }
    }

    private static T SafeDeserialize<T>(string? json, T fallback, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Console.WriteLine($"[DOC] {fieldName} empty, using fallback.");
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json) ?? fallback;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[DOC] {fieldName} invalid JSON: {ex.Message}");
            throw new InvalidOperationException($"JSON inválido no campo {fieldName}: {ex.Message}", ex);
        }
    }

    private static async Task WithTimeoutAsync(Task task, TimeSpan timeout, string timeoutMessage)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed != task)
        {
            throw new TimeoutException(timeoutMessage);
        }
        
        await task;
    }

    private static async Task<T> WithTimeoutAsync<T>(Task<T> task, TimeSpan timeout, string timeoutMessage)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed != task)
        {
            throw new TimeoutException(timeoutMessage);
        }

        return await task;
    }

    private static string? ReadLogoBase64()
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Nanopaint_logo.png");
        if (!File.Exists(logoPath))
        {
            Console.WriteLine($"[DOC] Logo nao encontrado: {logoPath}");
            return null;
        }

        var bytes = File.ReadAllBytes(logoPath);
        var base64 = Convert.ToBase64String(bytes);
        var mime = "image/png";
        return $"data:{mime};base64,{base64}";
    }

    private void Report(string message, double? percent = null)
    {
        Console.WriteLine($"[DOC][PROGRESS] {(percent.HasValue ? $"{percent:0}% - " : string.Empty)}{message}");
        _progress.Report(message, percent);
    }
}
