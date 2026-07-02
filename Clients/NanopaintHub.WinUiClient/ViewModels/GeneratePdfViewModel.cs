using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Features.SdsGeneration;
using NanopaintHub.Application.Features.TdsGeneration;
using NanopaintHub.Application.Features.DocumentReleases;
using NanopaintHub.Domain.Entities;
using Windows.Storage.Pickers;
using Windows.Storage;
using NanopaintHub_WinUIClient.Services;
using Windows.System;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class GeneratePdfViewModel : ObservableObject
{
    private readonly IProductRepository _productRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly ITestBatchRepository _testBatchRepository;
    private readonly GenerateTdsUseCase _generateTdsUseCase;
    private readonly GenerateSdsUseCase _generateSdsUseCase;
    private readonly SaveDocumentReleaseUseCase _saveDocumentReleaseUseCase;
    private readonly IDocumentGeneratorService _documentGeneratorService;
    private readonly IHtmlEngineService _htmlEngineService;
    private readonly IChromiumTinyMceEditorService _editorService;
    private readonly PdfPreviewService _pdfPreviewService;
    private readonly Microsoft.Extensions.Options.IOptionsMonitor<NanopaintHub.Infrastructure.Documents.DependencyInjection.PdfGeneratorOptions> _pdfOptions;
    private readonly INavigationService _navigationService;

    // Constructor for DI
    public GeneratePdfViewModel(
        IProductRepository productRepository,
        ITemplateRepository templateRepository,
        ITestBatchRepository testBatchRepository,
        GenerateTdsUseCase generateTdsUseCase,
        GenerateSdsUseCase generateSdsUseCase,
        SaveDocumentReleaseUseCase saveDocumentReleaseUseCase,
        IDocumentGeneratorService documentGeneratorService,
        IHtmlEngineService htmlEngineService,
        IChromiumTinyMceEditorService editorService,
        PdfPreviewService pdfPreviewService,
        Microsoft.Extensions.Options.IOptionsMonitor<NanopaintHub.Infrastructure.Documents.DependencyInjection.PdfGeneratorOptions> pdfOptions,
        INavigationService navigationService)
    {
        _productRepository = productRepository;
        _templateRepository = templateRepository;
        _testBatchRepository = testBatchRepository;
        _generateTdsUseCase = generateTdsUseCase;
        _generateSdsUseCase = generateSdsUseCase;
        _saveDocumentReleaseUseCase = saveDocumentReleaseUseCase;
        _documentGeneratorService = documentGeneratorService;
        _htmlEngineService = htmlEngineService;
        _editorService = editorService;
        _pdfPreviewService = pdfPreviewService;
        _pdfOptions = pdfOptions;
        _navigationService = navigationService;

        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _pdfOptions.OnChange((options, name) =>
        {
            if (dispatcher != null)
            {
                dispatcher.TryEnqueue(() => EvaluateQuestPdfEngine());
            }
            else
            {
                EvaluateQuestPdfEngine();
            }
        });
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }


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

    partial void OnSelectedProductChanged(Product? value) => EditedHtmlContent = null;

    [ObservableProperty]
    private DocumentTemplate? _selectedTemplate;

    partial void OnSelectedTemplateChanged(DocumentTemplate? value) => EditedHtmlContent = null;

    [ObservableProperty]
    private string _documentType = "TDS";

    partial void OnDocumentTypeChanged(string value)
    {
        EditedHtmlContent = null;
        if (Templates != null)
        {
            var filtered = Templates.Where(t => t.DocumentType == value);
            FilteredTemplates = new ObservableCollection<DocumentTemplate>(filtered);
        }
        EvaluateQuestPdfEngine();
    }

    [ObservableProperty]
    private bool _isQuestPdfSelected;

    [ObservableProperty]
    private bool _isNotQuestPdfSelected;

    private void EvaluateQuestPdfEngine()
    {
        var engine = DocumentType == "SDS" 
            ? _pdfOptions.CurrentValue.SdsEngine 
            : _pdfOptions.CurrentValue.TdsEngine;
        
        IsQuestPdfSelected = string.Equals(engine, "questpdf", StringComparison.OrdinalIgnoreCase);
        IsNotQuestPdfSelected = !IsQuestPdfSelected;
    }

    [ObservableProperty]
    private string? _editedHtmlContent;

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

    [ObservableProperty]
    private ObservableCollection<BitmapImage> _pdfPreviewImages = new();

    [ObservableProperty]
    private bool _isPdfPreviewVisible;

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
            if (!string.IsNullOrWhiteSpace(EditedHtmlContent))
            {
                pdfBytes = await _htmlEngineService.GeneratePdfFromRawBodyAsync(EditedHtmlContent, SelectedTemplate, settings);
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
                
                // Audit Trail: Gravar Document Release
                await _saveDocumentReleaseUseCase.ExecuteAsync(new SaveDocumentReleaseCommand(
                    SelectedProduct.Id,
                    SelectedProduct.Name,
                    DocumentType,
                    SelectedProduct.DocumentVersion.ToString(),
                    file.Path,
                    SelectedTemplate?.BuilderJson ?? ""
                ));

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

    [RelayCommand]
    private async Task PreviewPdfExternalAsync()
    {
        if (SelectedProduct == null || SelectedTemplate == null)
        {
            StatusMessage = "Selecione um produto e um template para pré-visualizar o PDF.";
            return;
        }
        IsLoading = true;
        StatusMessage = "Gerando pré‑visualização PDF...";
        try
        {
            byte[] pdfBytes;
            var settings = new PdfGenerationSettings
            {
                PageFormat = PageFormat == "Carta" ? "Letter" : "A4",
                PageOrientation = PageOrientation == "Paisagem" ? "Landscape" : "Portrait"
            };

            if (!string.IsNullOrWhiteSpace(EditedHtmlContent))
            {
                pdfBytes = await _htmlEngineService.GeneratePdfFromRawBodyAsync(EditedHtmlContent, SelectedTemplate, settings);
            }
            else if (DocumentType == "TDS")
            {
                pdfBytes = await _generateTdsUseCase.ExecuteAsync(new GenerateTdsQuery(SelectedProduct.Id, SelectedTemplate.Id, null, true, settings));
            }
            else
            {
                pdfBytes = await _generateSdsUseCase.ExecuteAsync(new GenerateSdsQuery(SelectedProduct.Id, SelectedTemplate.Id, settings));
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
            await File.WriteAllBytesAsync(tempPath, pdfBytes);
            var file = await StorageFile.GetFileFromPathAsync(tempPath);
            await Launcher.LaunchFileAsync(file);
            StatusMessage = "Pré‑visualização aberta no visualizador padrão.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao gerar pré‑visualização PDF: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PreviewPdfImageAsync()
    {
        if (SelectedProduct == null || SelectedTemplate == null)
        {
            StatusMessage = "Selecione um produto e um template para pré‑visualizar o PDF.";
            return;
        }
        IsLoading = true;
        StatusMessage = "Gerando pré‑visualização PDF...";
        try
        {
            byte[] pdfBytes;
            var settings = new PdfGenerationSettings
            {
                PageFormat = PageFormat == "Carta" ? "Letter" : "A4",
                PageOrientation = PageOrientation == "Paisagem" ? "Landscape" : "Portrait"
            };

            if (!string.IsNullOrWhiteSpace(EditedHtmlContent))
            {
                pdfBytes = await _htmlEngineService.GeneratePdfFromRawBodyAsync(EditedHtmlContent, SelectedTemplate, settings);
            }
            else if (DocumentType == "TDS")
            {
                pdfBytes = await _generateTdsUseCase.ExecuteAsync(new GenerateTdsQuery(SelectedProduct.Id, SelectedTemplate.Id, null, true, settings));
            }
            else
            {
                pdfBytes = await _generateSdsUseCase.ExecuteAsync(new GenerateSdsQuery(SelectedProduct.Id, SelectedTemplate.Id, settings));
            }

            var tempPdfPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            await File.WriteAllBytesAsync(tempPdfPath, pdfBytes);
            var imagePaths = await _pdfPreviewService.RenderPdfToImagesAsync(tempPdfPath);
            PdfPreviewImages.Clear();
            foreach (var imgPath in imagePaths)
            {
                var bitmap = new BitmapImage(new Uri(imgPath));
                PdfPreviewImages.Add(bitmap);
            }
            IsPdfPreviewVisible = true;
            StatusMessage = "Pré‑visualização carregada.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao gerar pré‑visualização PDF: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EditContentWithTinyMceAsync()
    {
        if (SelectedProduct == null || SelectedTemplate == null)
        {
            StatusMessage = "Selecione um produto e um template antes de ajustar o conteúdo.";
            return;
        }

        IsLoading = true;
        StatusMessage = "A preparar editor de conteúdo...";

        try
        {
            string htmlToEdit;
            if (!string.IsNullOrWhiteSpace(EditedHtmlContent))
            {
                htmlToEdit = EditedHtmlContent;
            }
            else
            {
                // We use the raw body so TinyMCE doesn't mangle <head> or PagedJs scripts
                htmlToEdit = await _htmlEngineService.GenerateRawBodyHtmlAsync(SelectedProduct, SelectedTemplate, DocumentType == "SDS", null);
            }

            var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
            StatusMessage = "A abrir editor...";

            var result = await _editorService.OpenEditorAsync(htmlToEdit, assetsDir, string.Empty);

            if (result.Applied && !string.IsNullOrWhiteSpace(result.Html))
            {
                EditedHtmlContent = result.Html;
                StatusMessage = "Alterações aplicadas. A atualizar pré-visualização...";
                // Automagically trigger preview to show changes
                await PreviewPdfImageAsync();
            }
            else
            {
                StatusMessage = "Edição cancelada.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao abrir editor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Initialize the ViewModel by loading products and templates
    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "Carregando dados...";
        try
        {
            // Load products
            var products = await _productRepository.GetAllAsync();
            Products = new ObservableCollection<Product>(products);

            // Load templates
            var templates = await _templateRepository.GetAllAsync();
            Templates = new ObservableCollection<DocumentTemplate>(templates);

            // Initialize filtered templates based on current document type
            var filtered = templates.Where(t => t.DocumentType == DocumentType);
            FilteredTemplates = new ObservableCollection<DocumentTemplate>(filtered);
            
            EvaluateQuestPdfEngine();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao inicializar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

