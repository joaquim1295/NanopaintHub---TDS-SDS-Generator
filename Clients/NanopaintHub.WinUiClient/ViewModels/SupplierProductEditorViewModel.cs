using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class SupplierProductEditorViewModel : ObservableObject, IDisposable
{
    private readonly ISupplierProductRepository _supplierProductRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISaveSupplierProductUseCase _saveSupplierProductUseCase;
    private readonly IEchaSubstanceRepository _echaSubstanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly ISdsPdfExtractorService _sdsPdfExtractorService;

    [ObservableProperty]
    private string _pageTitle = "Nova Matéria-Prima";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    // Form fields
    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _productCode = string.Empty;

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private string _sdsFilePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private ObservableCollection<SupplierCompositionViewModel> _compositions = new();

    // ECHA Suggestion fields
    [ObservableProperty]
    private string _substanceSearchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<EchaSubstance> _suggestedSubstances = new();

    [ObservableProperty]
    private EchaSubstance? _selectedSubstance;

    [ObservableProperty]
    private double _newPercentage = 0;

    [ObservableProperty]
    private double _totalPercentage = 0;

    private Guid? _editingProductId;
    private SupplierProduct? _editingProduct;

    public SupplierProductEditorViewModel(
        ISupplierProductRepository supplierProductRepository,
        ISupplierRepository supplierRepository,
        ISdsPdfExtractorService sdsPdfExtractorService,
        ISaveSupplierProductUseCase saveSupplierProductUseCase,
        IEchaSubstanceRepository echaSubstanceRepository,
        IUnitOfWork unitOfWork,
        INavigationService navigationService)
    {
        _supplierProductRepository = supplierProductRepository;
        _supplierRepository = supplierRepository;
        _sdsPdfExtractorService = sdsPdfExtractorService;
        _saveSupplierProductUseCase = saveSupplierProductUseCase;
        _echaSubstanceRepository = echaSubstanceRepository;
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync(Guid? productId = null)
    {
        IsLoading = true;
        HasError = false;

        try
        {
            var suppliers = await _supplierRepository.GetAllAsync();
            Suppliers.Clear();
            foreach (var s in suppliers)
            {
                Suppliers.Add(s);
            }

            if (productId.HasValue && productId.Value != Guid.Empty)
            {
                _editingProductId = productId.Value;
                PageTitle = "Editar Matéria-Prima";
                
                _editingProduct = await _supplierProductRepository.GetByIdAsync(_editingProductId.Value);
                if (_editingProduct != null)
                {
                    ProductName = _editingProduct.Name;
                    ProductCode = _editingProduct.ProductCode;
                    SdsFilePath = _editingProduct.SdsFilePath;
                    SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == _editingProduct.SupplierId);
                    
                    Compositions.Clear();
                    if (_editingProduct.Compositions != null)
                    {
                        foreach (var comp in _editingProduct.Compositions)
                        {
                            var vm = new SupplierCompositionViewModel(comp);
                            vm.PropertyChanged += (s, e) => {
                                if (e.PropertyName == nameof(SupplierCompositionViewModel.Percentage))
                                {
                                    UpdateTotalPercentage();
                                }
                            };
                            Compositions.Add(vm);
                        }
                    }
                    UpdateTotalPercentage();
                }
            }
            else
            {
                _editingProductId = null;
                _editingProduct = null;
                PageTitle = "Nova Matéria-Prima";
                ProductName = string.Empty;
                ProductCode = string.Empty;
                SdsFilePath = string.Empty;
                SelectedSupplier = null;
                Compositions.Clear();
                UpdateTotalPercentage();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao inicializar: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductName))
        {
            ShowError("O nome da matéria-prima é obrigatório.");
            return;
        }

        if (SelectedSupplier == null)
        {
            ShowError("É obrigatório selecionar um fornecedor.");
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            SupplierProduct productToSave;

            if (_editingProductId.HasValue && _editingProduct != null)
            {
                // Update existing product
                productToSave = _editingProduct;
            }
            else
            {
                // Create new product
                productToSave = new SupplierProduct();
            }

            productToSave.Name = ProductName;
            productToSave.ProductCode = ProductCode;
            productToSave.SupplierId = SelectedSupplier.Id;
            productToSave.SdsFilePath = SdsFilePath;

            bool isNew = !(_editingProductId.HasValue && _editingProduct != null);
            await _saveSupplierProductUseCase.ExecuteAsync(
                productToSave, 
                SdsFilePath, 
                Compositions.Select(c => c.Model).ToList(),
                isNew);

            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao gravar: {ex.Message}");
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
    private async Task BrowseSdsAsync()
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        
        var app = (Microsoft.UI.Xaml.Application.Current as App);
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            SdsFilePath = file.Path;
            // Immediately extract SDS data to populate compositions
            var extracted = await _sdsPdfExtractorService.ExtractSdsDataAsync(SdsFilePath);
            foreach (var casData in extracted.CasNumbers)
            {
                var sub = await _echaSubstanceRepository.GetByCasAsync(casData.CasNumber);
                if (sub == null)
                {
                    sub = new NanopaintHub.Domain.Entities.EchaSubstance
                    {
                        CasNumber = casData.CasNumber,
                        ChemicalName = $"Substância Importada (CAS: {casData.CasNumber})"
                    };
                    await _echaSubstanceRepository.AddAsync(sub);
                }
                // Add hazards if any
                if (casData.ExtractedHazards != null && casData.ExtractedHazards.Any())
                {
                    foreach (var hazard in casData.ExtractedHazards)
                    {
                        if (!sub.Hazards.Any(h => h.PhraseCode == hazard))
                        {
                            sub.Hazards.Add(new NanopaintHub.Domain.Entities.SubstanceHazard
                            {
                                Substance = sub,
                                PhraseCode = hazard
                            });
                        }
                    }
                }
                // Update or add composition entry in UI collection
                var existingComp = Compositions.FirstOrDefault(c => c.EchaSubstanceId == sub.Id);
                if (existingComp != null)
                {
                    existingComp.Percentage = casData.ParsedPercentage;
                    existingComp.PercentageText = casData.RawPercentageText;
                    existingComp.OriginalPercentageText = casData.RawPercentageText;
                    // Reset manual flag since this is a fresh import from PDF
                    existingComp.IsManuallyEdited = false;
                }
                else
                {
                    var newComp = new NanopaintHub.Domain.Entities.SupplierProductComposition
                    {
                        EchaSubstanceId = sub.Id,
                        EchaSubstance = sub,
                        Percentage = casData.ParsedPercentage,
                        PercentageText = casData.RawPercentageText,
                        OriginalPercentageText = casData.RawPercentageText,
                        IsManuallyEdited = false
                    };
                    var vm = new SupplierCompositionViewModel(newComp);
                    vm.PropertyChanged += (s, e) => {
                        if (e.PropertyName == nameof(SupplierCompositionViewModel.Percentage)) UpdateTotalPercentage();
                    };
                    Compositions.Add(vm);
                }
            }
            UpdateTotalPercentage();
        }
    }

    // AutoSuggest Box Logic
    public async Task PerformSubstanceSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            SuggestedSubstances.Clear();
            return;
        }

        try
        {
            var results = await _echaSubstanceRepository.SearchByNameAsync(query, 10);
            SuggestedSubstances.Clear();
            foreach (var r in results)
            {
                SuggestedSubstances.Add(r);
            }
        }
        catch
        {
            // Ignore search errors
        }
    }

    [RelayCommand]
    private void AddComposition()
    {
        if (SelectedSubstance == null)
        {
            ShowError("Selecione uma substância química válida da lista.");
            return;
        }

        if (NewPercentage <= 0)
        {
            ShowError("A percentagem tem de ser maior que 0.");
            return;
        }

        if (Compositions.Any(c => c.EchaSubstanceId == SelectedSubstance.Id))
        {
            ShowError("Esta substância já existe na composição.");
            return;
        }

        var newTotal = TotalPercentage + NewPercentage;
        if (newTotal > 100)
        {
            ShowError($"Adicionar {NewPercentage}% excede o limite de 100% (Total: {newTotal}%).");
            return;
        }

        var comp = new SupplierProductComposition
        {
            EchaSubstanceId = SelectedSubstance.Id,
            EchaSubstance = SelectedSubstance,
            Percentage = NewPercentage,
            PercentageText = $"{NewPercentage}%",
            IsManuallyEdited = true
        };

        var vm = new SupplierCompositionViewModel(comp);
        vm.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(SupplierCompositionViewModel.Percentage)) UpdateTotalPercentage();
        };

        Compositions.Add(vm);
        UpdateTotalPercentage();
        HasError = false;

        // Reset fields
        SelectedSubstance = null;
        SubstanceSearchQuery = string.Empty;
        NewPercentage = 0;
    }

    [RelayCommand]
    private void RemoveComposition(SupplierCompositionViewModel comp)
    {
        if (comp != null && Compositions.Contains(comp))
        {
            Compositions.Remove(comp);
            UpdateTotalPercentage();
        }
    }

    private void UpdateTotalPercentage()
    {
        TotalPercentage = Compositions.Sum(c => c.Percentage);
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    [RelayCommand]
    private async Task AddSupplierAsync()
    {
        var window = (Microsoft.UI.Xaml.Application.Current as NanopaintHub_WinUIClient.App)?.MainWindow;
        if (window == null) return;

        var nameInput = new Microsoft.UI.Xaml.Controls.TextBox { Header = "Nome do Fornecedor *", PlaceholderText = "Ex: BASF" };
        var countryInput = new Microsoft.UI.Xaml.Controls.TextBox { Header = "País", PlaceholderText = "Ex: Alemanha", Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0) };
        var contactInput = new Microsoft.UI.Xaml.Controls.TextBox { Header = "Contacto Técnico", PlaceholderText = "Ex: sds-support@basf.com", Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0) };

        var panel = new Microsoft.UI.Xaml.Controls.StackPanel();
        panel.Children.Add(nameInput);
        panel.Children.Add(countryInput);
        panel.Children.Add(contactInput);

        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "Novo Fornecedor",
            Content = panel,
            PrimaryButtonText = "Criar",
            CloseButtonText = "Cancelar",
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
            XamlRoot = window.Content.XamlRoot
        };

        try
        {
            var result = await dialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameInput.Text))
            {
                var name = nameInput.Text.Trim();
                
                // Check duplicates
                var existing = Suppliers.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    SelectedSupplier = existing;
                    return;
                }

                var newSupplier = new Supplier
                {
                    Name = name,
                    Country = countryInput.Text.Trim(),
                    TechnicalContact = contactInput.Text.Trim()
                };

                await _supplierRepository.AddAsync(newSupplier);
                await _unitOfWork.CommitAsync();

                Suppliers.Add(newSupplier);
                SelectedSupplier = newSupplier;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao adicionar fornecedor: {ex.Message}");
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

public class SupplierCompositionViewModel : ObservableObject
{
    private readonly SupplierProductComposition _model;

    public SupplierCompositionViewModel(SupplierProductComposition model)
    {
        _model = model;
    }

    public SupplierProductComposition Model => _model;

    public Guid EchaSubstanceId => _model.EchaSubstanceId;
    public EchaSubstance? EchaSubstance => _model.EchaSubstance;
    
    public string? PercentageText 
    {
        get => _model.PercentageText;
        set
        {
            if (_model.PercentageText != value)
            {
                _model.PercentageText = value;
                OnPropertyChanged();
            }
        }
    }

    public string? OriginalPercentageText 
    {
        get => _model.OriginalPercentageText;
        set
        {
            if (_model.OriginalPercentageText != value)
            {
                _model.OriginalPercentageText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsManuallyEdited
    {
        get => _model.IsManuallyEdited;
        set
        {
            if (_model.IsManuallyEdited != value)
            {
                _model.IsManuallyEdited = value;
                OnPropertyChanged();
            }
        }
    }

    public double Percentage
    {
        get => _model.Percentage;
        set
        {
            if (_model.Percentage != value)
            {
                _model.Percentage = value;
                IsManuallyEdited = true; // Mark as manually edited when changed
                PercentageText = $"{value}%";
                OnPropertyChanged();
                OnPropertyChanged(nameof(PercentageText));
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
            // If it's invalid text, we just ignore it and let it stay the previous value.
            OnPropertyChanged();
        }
    }
}
