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
    private readonly GenerateTdsUseCase _generateTdsUseCase;
    private readonly GenerateSdsUseCase _generateSdsUseCase;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<DocumentTemplate> _templates = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private DocumentTemplate? _selectedTemplate;

    [ObservableProperty]
    private string _documentType = "TDS";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    public GeneratePdfViewModel(
        IProductRepository productRepository, 
        ITemplateRepository templateRepository,
        GenerateTdsUseCase generateTdsUseCase,
        GenerateSdsUseCase generateSdsUseCase)
    {
        _productRepository = productRepository;
        _templateRepository = templateRepository;
        _generateTdsUseCase = generateTdsUseCase;
        _generateSdsUseCase = generateSdsUseCase;
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
        UpdateTemplatesFilter();
    }

    private void UpdateTemplatesFilter()
    {
        SelectedTemplate = Templates.FirstOrDefault(t => t.DocumentType == DocumentType);
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
            byte[] pdfBytes;
            if (DocumentType == "TDS")
            {
                pdfBytes = await _generateTdsUseCase.ExecuteAsync(new GenerateTdsQuery(SelectedProduct.Id, SelectedTemplate.Id));
            }
            else
            {
                pdfBytes = await _generateSdsUseCase.ExecuteAsync(new GenerateSdsQuery(SelectedProduct.Id, SelectedTemplate.Id));
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
                StatusMessage = $"Salvo em: {file.Path}";
            }
            else
            {
                StatusMessage = "Cancelado.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
