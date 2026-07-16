using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class ProductEditorViewModel : ObservableObject
{
    private readonly IProductRepository _productRepository;
    private readonly INavigationService _navigationService;
    private readonly ISubstanceSearchService _searchService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private Product? _product;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "Novo Produto";

    // Collections for UI binding
    public ObservableCollection<StringItem> Features { get; } = new();
    public ObservableCollection<KeyValuePairItem> LiquidProperties { get; } = new();
    public ObservableCollection<KeyValuePairItem> MechanicalProperties { get; } = new();
    public ObservableCollection<KeyValuePairItem> CustomSensorSpecs { get; } = new();
    public ObservableCollection<CompositionViewModel> Compositions { get; } = new();

    [ObservableProperty]
    private IEnumerable<NanopaintHub.Application.DTOs.FederatedSearchResult> _substanceSearchResults = Array.Empty<NanopaintHub.Application.DTOs.FederatedSearchResult>();

    [ObservableProperty]
    private TdsSensorSpecs _sensorSpecs = new();

    [ObservableProperty]
    private string _compositionsValidationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasCompositionsError = false;

    public void ValidateCompositions()
    {
        var total = Compositions.Sum(c => c.Percentage);
        if (Compositions.Any(c => c.Percentage <= 0))
        {
            HasCompositionsError = true;
            CompositionsValidationMessage = "A percentagem não pode ser 0 ou inferior.";
        }
        else if (total > 100)
        {
            HasCompositionsError = true;
            CompositionsValidationMessage = $"O total excede 100% (Atual: {total}%).";
        }
        else
        {
            HasCompositionsError = false;
            CompositionsValidationMessage = string.Empty;
        }
    }

    public ProductEditorViewModel(
        IProductRepository productRepository, 
        INavigationService navigationService,
        ISubstanceSearchService searchService,
        IUnitOfWork unitOfWork)
    {
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _productRepository = productRepository;
        _navigationService = navigationService;
        _searchService = searchService;
        _unitOfWork = unitOfWork;
    }

    public async Task InitializeAsync(Guid? productId)
    {
        IsLoading = true;
        try
        {
            if (productId.HasValue && productId.Value != Guid.Empty)
            {
                Product = await _productRepository.GetByIdAsync(productId.Value);
                PageTitle = $"Editar Produto - {Product?.Code}";
            }
            else
            {
                Product = new Product 
                { 
                    Name = "",
                    Code = "",
                    FeaturesJson = "[]",
                    LiquidPropertiesJson = "{}",
                    MechanicalPropertiesJson = "{}",
                    TdsSensorSpecsJson = "{}"
                };
                PageTitle = "Novo Produto";
            }

            LoadCollectionsFromProduct();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadCollectionsFromProduct()
    {
        if (Product == null) return;

        Features.Clear();
        LiquidProperties.Clear();
        MechanicalProperties.Clear();
        CustomSensorSpecs.Clear();
        Compositions.Clear();

        if (Product.Compositions != null)
        {
            foreach (var comp in Product.Compositions)
            {
                Compositions.Add(new CompositionViewModel(comp));
            }
        }
        ValidateCompositions();

        try
        {
            var featuresList = string.IsNullOrWhiteSpace(Product.FeaturesJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(Product.FeaturesJson);
            
            if (featuresList != null)
            {
                foreach (var f in featuresList) Features.Add(new StringItem(f));
            }

            var liquidDict = string.IsNullOrWhiteSpace(Product.LiquidPropertiesJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(Product.LiquidPropertiesJson);
            
            if (liquidDict != null)
            {
                foreach (var kvp in liquidDict) LiquidProperties.Add(new KeyValuePairItem(kvp.Key, kvp.Value));
            }

            var mechDict = string.IsNullOrWhiteSpace(Product.MechanicalPropertiesJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(Product.MechanicalPropertiesJson);
            
            if (mechDict != null)
            {
                foreach (var kvp in mechDict) MechanicalProperties.Add(new KeyValuePairItem(kvp.Key, kvp.Value));
            }

            SensorSpecs = string.IsNullOrWhiteSpace(Product.TdsSensorSpecsJson)
                ? new TdsSensorSpecs()
                : JsonSerializer.Deserialize<TdsSensorSpecs>(Product.TdsSensorSpecsJson) ?? new TdsSensorSpecs();
            
            if (SensorSpecs.ExtraSpecs != null)
            {
                foreach (var kvp in SensorSpecs.ExtraSpecs) CustomSensorSpecs.Add(new KeyValuePairItem(kvp.Key, kvp.Value));
            }
        }
        catch (Exception ex)
        {
            // Log or handle deserialization error gracefully
            System.Diagnostics.Debug.WriteLine($"Error parsing JSON: {ex.Message}");
        }
    }

    private void SaveCollectionsToProduct()
    {
        if (Product == null) return;

        Product.FeaturesJson = JsonSerializer.Serialize(Features.Select(f => f.Value).Where(f => !string.IsNullOrWhiteSpace(f)).ToList());
        
        var liquidDict = LiquidProperties
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .ToDictionary(p => p.Key, p => p.Value);
        Product.LiquidPropertiesJson = JsonSerializer.Serialize(liquidDict);

        var mechDict = MechanicalProperties
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .ToDictionary(p => p.Key, p => p.Value);
        Product.MechanicalPropertiesJson = JsonSerializer.Serialize(mechDict);
        
        var customSensorDict = CustomSensorSpecs
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .ToDictionary(p => p.Key, p => p.Value);
        
        if (SensorSpecs == null) SensorSpecs = new TdsSensorSpecs();
        SensorSpecs.ExtraSpecs = customSensorDict;

        Product.TdsSensorSpecsJson = JsonSerializer.Serialize(SensorSpecs);

        if (Product.Compositions == null) 
        {
            Product.Compositions = new List<ProductComposition>();
        }

        var newModels = Compositions.Select(c => c.Model).ToList();
        var toRemove = Product.Compositions.Where(c => !newModels.Contains(c)).ToList();
        foreach (var c in toRemove) Product.Compositions.Remove(c);

        foreach (var m in newModels)
        {
            if (!Product.Compositions.Contains(m))
                Product.Compositions.Add(m);
        }
    }

    [RelayCommand]
    private void AddFeature() => Features.Add(new StringItem(""));

    [RelayCommand]
    private void RemoveFeature(StringItem item)
    {
        if (item != null) Features.Remove(item);
    }

    [RelayCommand]
    private void AddLiquidProperty() => LiquidProperties.Add(new KeyValuePairItem("", ""));

    [RelayCommand]
    private void RemoveLiquidProperty(KeyValuePairItem item)
    {
        if (item != null) LiquidProperties.Remove(item);
    }

    [RelayCommand]
    private void AddMechanicalProperty() => MechanicalProperties.Add(new KeyValuePairItem("", ""));

    [RelayCommand]
    private void RemoveMechanicalProperty(KeyValuePairItem item)
    {
        if (item != null) MechanicalProperties.Remove(item);
    }

    [RelayCommand]
    private void AddCustomSensorSpec() => CustomSensorSpecs.Add(new KeyValuePairItem("", ""));

    [RelayCommand]
    private void RemoveCustomSensorSpec(KeyValuePairItem item)
    {
        if (item != null) CustomSensorSpecs.Remove(item);
    }

    [RelayCommand]
    private async Task SearchSubstancesAsync(string query)
    {
        try
        {
            var results = await _searchService.SearchAsync(query);
            var resultsList = results.ToList();
            
            // Always append the "Import from PDF" action at the end
            resultsList.Add(new NanopaintHub.Application.DTOs.FederatedSearchResult
            {
                DisplayName = "📄 Importar SDS de um Fornecedor (PDF)",
                Description = string.IsNullOrWhiteSpace(query) ? "Adicionar uma nova matéria-prima lendo a sua Ficha de Segurança." : $"Não encontrou '{query}'? Clique aqui para importar o PDF.",
                IsImportAction = true,
                TypedQuery = query
            });

            _dispatcherQueue.TryEnqueue(() =>
            {
                SubstanceSearchResults = resultsList;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao pesquisar substâncias: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddComposition(NanopaintHub.Application.DTOs.FederatedSearchResult result)
    {
        if (result == null) return;
        
        if (result.EchaSubstance != null)
        {
            if (Compositions.Any(c => c.Model.EchaSubstanceId == result.EchaSubstance.Id)) return; // Já existe

            Compositions.Add(new CompositionViewModel(new ProductComposition
            {
                EchaSubstanceId = result.EchaSubstance.Id,
                EchaSubstance = result.EchaSubstance,
                ProductId = Product?.Id ?? Guid.Empty,
                Percentage = 0,
                ExtraMetadataJson = "{}"
            }));
        }
        else if (result.SupplierProduct != null)
        {
            if (Compositions.Any(c => c.Model.SupplierProductId == result.SupplierProduct.Id)) return; // Já existe

            Compositions.Add(new CompositionViewModel(new ProductComposition
            {
                SupplierProductId = result.SupplierProduct.Id,
                SupplierProduct = result.SupplierProduct,
                ProductId = Product?.Id ?? Guid.Empty,
                Percentage = 0,
                ExtraMetadataJson = "{}"
            }));
        }
        
        ValidateCompositions();
    }

    [RelayCommand]
    private void RemoveComposition(CompositionViewModel comp)
    {
        if (comp != null) 
        {
            Compositions.Remove(comp);
            ValidateCompositions();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Product == null) return;

        if (Compositions.Any(c => c.Percentage <= 0))
        {
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Percentagem Inválida",
                    Content = "A percentagem de um componente não pode ser 0 ou inferior. Por favor, introduza um valor válido para todos os componentes.",
                    CloseButtonText = "OK",
                    XamlRoot = window.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            return;
        }

        var totalPercentage = Compositions.Sum(c => c.Percentage);
        if (totalPercentage > 100)
        {
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Percentagem Total Inválida",
                    Content = $"A soma das percentagens dos componentes é de {totalPercentage}%. O total não deve ultrapassar os 100%.",
                    CloseButtonText = "OK",
                    XamlRoot = window.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            return;
        }

        SaveCollectionsToProduct();

        IsLoading = true;
        try
        {
            var existing = await _productRepository.GetByIdAsync(Product.Id);
            if (existing != null)
            {
                Product.MarkAsModified();
                await _productRepository.UpdateAsync(Product);
            }
            else
            {
                await _productRepository.AddAsync(Product);
            }

            await _unitOfWork.CommitAsync();

            _navigationService.GoBack();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task SelectPdfAsync()
    {
        if (Product == null) return;
        
        var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
        if (window == null) return;

        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            Product.ExposureScenarioPdfPath = file.Path;
            OnPropertyChanged(nameof(Product));
        }
    }
}

public partial class CompositionViewModel : ObservableObject
{
    private readonly ProductComposition _model;

    public CompositionViewModel(ProductComposition model)
    {
        _model = model;
    }

    public ProductComposition Model => _model;

    public string DisplayName => _model.DisplayName;
    public string DisplayDescription => _model.DisplayDescription;
    public SupplierProduct? SupplierProduct => _model.SupplierProduct;
    public EchaSubstance? EchaSubstance => _model.EchaSubstance;
    
    public double Percentage
    {
        get => _model.Percentage;
        set
        {
            if (_model.Percentage != value)
            {
                _model.Percentage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InnerComponents));
                OnPropertyChanged(nameof(PercentageInput));
            }
        }
    }

    public string PercentageInput
    {
        get => Percentage.ToString();
        set
        {
            if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                Percentage = parsed;
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                Percentage = 0;
            }
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<InnerComponentViewModel> InnerComponents
    {
        get
        {
            if (_model.SupplierProduct?.Compositions == null) return Array.Empty<InnerComponentViewModel>();
            
            return _model.SupplierProduct.Compositions.Select(c => new InnerComponentViewModel
            {
                ChemicalName = c.EchaSubstance?.ChemicalName ?? "Desconhecido",
                EffectivePercentage = Math.Round(_model.Percentage * c.Percentage / 100.0, 2)
            }).ToList();
        }
    }
}

public class InnerComponentViewModel
{
    public string ChemicalName { get; set; } = string.Empty;
    public double EffectivePercentage { get; set; }
}
