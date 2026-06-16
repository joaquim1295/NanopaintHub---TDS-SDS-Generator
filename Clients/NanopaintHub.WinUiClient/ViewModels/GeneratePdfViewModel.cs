using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Features.SdsGeneration;
using NanopaintHub.Application.Features.TdsGeneration;
using NanopaintHub.Domain.Entities;
using Windows.Storage.Pickers;
using Windows.Storage;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class GeneratePdfViewModel : ObservableObject
{
    private readonly IProductRepository _productRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly ITestBatchRepository _testBatchRepository;
    private readonly GenerateTdsUseCase _generateTdsUseCase;
    private readonly GenerateSdsUseCase _generateSdsUseCase;
    private readonly IDocumentGeneratorService _documentGeneratorService;
    private readonly IChromiumTinyMceEditorService _editorService;

    private string? _customPreviewHtml;

    public List<string> DocumentTypes { get; } = new() { "TDS", "SDS" };
    public List<string> PageFormats { get; } = new() { "A4", "Carta" };
    public List<string> PageOrientations { get; } = new() { "Retrato", "Paisagem" };

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<DocumentTemplate> _templates = new();

    [ObservableProperty]
    private ObservableCollection<DocumentTemplate> _filteredTemplates = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private DocumentTemplate? _selectedTemplate;

    [ObservableProperty]
    private string _documentType = "TDS";

    [ObservableProperty]
    private string _pageFormat = "A4";

    [ObservableProperty]
    private string _pageOrientation = "Retrato";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string? _savedFilePath;

    public Uri? SavedFileUri => string.IsNullOrWhiteSpace(SavedFilePath) ? null : new Uri(SavedFilePath);

    [ObservableProperty]
    private bool _hasSavedFile;

    public GeneratePdfViewModel(
        IProductRepository productRepository, 
        ITemplateRepository templateRepository,
        ITestBatchRepository testBatchRepository,
        GenerateTdsUseCase generateTdsUseCase,
        GenerateSdsUseCase generateSdsUseCase,
        IDocumentGeneratorService documentGeneratorService,
        IChromiumTinyMceEditorService editorService)
    {
        _productRepository = productRepository;
        _templateRepository = templateRepository;
        _testBatchRepository = testBatchRepository;
        _generateTdsUseCase = generateTdsUseCase;
        _generateSdsUseCase = generateSdsUseCase;
        _documentGeneratorService = documentGeneratorService;
        _editorService = editorService;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var prods = await _productRepository.GetAllAsync();
            Products.Clear();
            foreach (var p in prods) Products.Add(p);

            var temps = await _templateRepository.GetAllAsync();
            Templates.Clear();
            foreach (var t in temps) Templates.Add(t);
            
            SelectedProduct = Products.FirstOrDefault();
            UpdateTemplatesFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnDocumentTypeChanged(string value)
    {
        _customPreviewHtml = null; // Reset custom preview when type changes
        UpdateTemplatesFilter();
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        _customPreviewHtml = null; // Reset custom preview when product changes
    }

    partial void OnSelectedTemplateChanged(DocumentTemplate? value)
    {
        _customPreviewHtml = null; // Reset custom preview when template changes
    }

    private void UpdateTemplatesFilter()
    {
        FilteredTemplates.Clear();
        foreach (var t in Templates.Where(x => x.DocumentType == DocumentType))
        {
            FilteredTemplates.Add(t);
        }
        SelectedTemplate = FilteredTemplates.FirstOrDefault();
    }

    [RelayCommand]
    private async Task PreviewPdfAsync()
    {
        if (SelectedProduct == null || SelectedTemplate == null)
        {
            StatusMessage = "Selecione um produto e um template para pré-visualizar.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Gerando pré-visualização...";
        try
        {
            TestBatch? latestBatch = null;
            if (DocumentType == "TDS")
            {
                latestBatch = await _testBatchRepository.GetLatestByProductIdAsync(SelectedProduct.Id);
            }

            var html = await _documentGeneratorService.GenerateHtmlAsync(SelectedProduct, SelectedTemplate, DocumentType == "SDS", latestBatch);
            
            // Extract CSS for TinyMCE - Always prioritize the generated HTML's embedded CSS for accurate preview
            string cssToPass = "";
            if (html.Contains("<style", StringComparison.OrdinalIgnoreCase))
            {
                var startIdx = html.IndexOf("<style", StringComparison.OrdinalIgnoreCase);
                var endIdx = html.IndexOf("</style>", startIdx, StringComparison.OrdinalIgnoreCase);
                if (startIdx >= 0 && endIdx > startIdx)
                {
                    var closeBracket = html.IndexOf(">", startIdx);
                    if (closeBracket > 0 && closeBracket < endIdx)
                    {
                        cssToPass = html.Substring(closeBracket + 1, endIdx - closeBracket - 1);
                    }
                }
            }
            
            // Fallback to template's saved CSS if extraction failed
            if (string.IsNullOrWhiteSpace(cssToPass))
            {
                cssToPass = SelectedTemplate.CssStructure ?? "";
            }

            var assetsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
            var result = await _editorService.OpenEditorAsync(
                html, 
                assetsPath, 
                cssToPass, 
                ChromiumTemplateEditorMode.WordLike);

            if (result.Applied && !string.IsNullOrWhiteSpace(result.Html))
            {
                _customPreviewHtml = result.Html;
                StatusMessage = "Pré-visualização guardada para geração.";
            }
            else
            {
                StatusMessage = "Pré-visualização fechada sem guardar alterações.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro no preview: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GeneratePdfAsync()
    {
        if (SelectedProduct == null || SelectedTemplate == null)
        {
            StatusMessage = "Selecione um produto e um template.";
            return;
        }

        IsLoading = true;
        StatusMessage = "Gerando PDF...";
        try
        {
            var settings = new PdfGenerationSettings 
            { 
                PageFormat = PageFormat == "Carta" ? "Letter" : "A4", 
                PageOrientation = PageOrientation == "Paisagem" ? "Landscape" : "Portrait" 
            };

            byte[] pdfBytes;
            if (!string.IsNullOrWhiteSpace(_customPreviewHtml))
            {
                // Generate from the manually edited HTML preview
                pdfBytes = await _documentGeneratorService.GeneratePdfFromHtmlAsync(_customPreviewHtml, SelectedTemplate, settings);
            }
            else if (DocumentType == "TDS")
            {
                pdfBytes = await _generateTdsUseCase.ExecuteAsync(new GenerateTdsQuery(SelectedProduct.Id, SelectedTemplate.Id, null, true, settings));
            }
            else
            {
                pdfBytes = await _generateSdsUseCase.ExecuteAsync(new GenerateSdsQuery(SelectedProduct.Id, SelectedTemplate.Id, settings));
            }

            var savePicker = new FileSavePicker();
            
            // Get window handle for picker
            var window = (App.Current as App)?.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("PDF", new[] { ".pdf" });
            savePicker.SuggestedFileName = $"{SelectedProduct.Code}_{DocumentType}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await File.WriteAllBytesAsync(file.Path, pdfBytes);
                StatusMessage = "PDF gerado com sucesso.";
                SavedFilePath = file.Path;
                OnPropertyChanged(nameof(SavedFileUri));
                HasSavedFile = true;
            }
            else
            {
                StatusMessage = "Cancelado.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
            HasSavedFile = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
