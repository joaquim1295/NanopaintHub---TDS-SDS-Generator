namespace NanopaintHub.WinUiClient.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using Windows.System;
using System.IO;
using System.Collections.Generic;
using NanopaintHub_WinUIClient.Services;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly ISupplierProductRepository _supplierProductRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly PdfPreviewService _pdfPreviewService;
    
    [ObservableProperty]
    private ObservableCollection<SupplierProduct> _supplierProducts = new();
    
    [ObservableProperty]
    private ObservableCollection<SupplierGroup> _groupedSupplierProducts = new();

    [ObservableProperty]
    private bool _isGroupedBySupplier = false;

    [ObservableProperty]
    private bool _isCardView = true;

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                SearchCommand.Execute(null);
            }
        }
    }

    private int _sortMode = 0;
    public int SortMode
    {
        get => _sortMode;
        set
        {
            if (SetProperty(ref _sortMode, value))
            {
                IsGroupedBySupplier = value == 1;
            }
        }
    }

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPdfPreviewVisible;

    [ObservableProperty]
    private bool _isLoadingPreview;

    [ObservableProperty]
    private ObservableCollection<string> _pdfPreviewImages = new();

    // New Supplier management fields
    [ObservableProperty]
    private ObservableCollection<Supplier> _filteredSuppliers = new();

    [ObservableProperty]
    private bool _isLoadingSuppliers;

    private List<Supplier> _allSuppliers = new();

    private string _supplierSearchQuery = string.Empty;
    public string SupplierSearchQuery
    {
        get => _supplierSearchQuery;
        set
        {
            if (SetProperty(ref _supplierSearchQuery, value))
            {
                ApplySupplierSearch();
            }
        }
    }

    public SuppliersViewModel(
        ISupplierProductRepository supplierProductRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        INavigationService navigationService,
        PdfPreviewService pdfPreviewService)
    {
        _supplierProductRepository = supplierProductRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _pdfPreviewService = pdfPreviewService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            IsLoadingSuppliers = true;
            HasError = false;

            // Load products
            var list = await _supplierProductRepository.GetAllAsync(includeArchived: false);
            SupplierProducts = new ObservableCollection<SupplierProduct>(list.OrderBy(p => p.Name));
            ApplyGrouping();

            // Load suppliers
            var supList = await _supplierRepository.GetAllAsync();
            _allSuppliers = supList.OrderBy(s => s.Name).ToList();
            ApplySupplierSearch();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao carregar dados: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsLoadingSuppliers = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadDataAsync();
            return;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            var results = await _supplierProductRepository.SearchByNameAsync(SearchQuery, limit: 100, includeArchived: false);
            SupplierProducts = new ObservableCollection<SupplierProduct>(results.OrderBy(p => p.Name));
            ApplyGrouping();

        }
        catch (Exception ex)
        {
            ShowError($"Erro na pesquisa: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task OpenSdsAsync(SupplierProduct product)
    {
        if (product == null || string.IsNullOrWhiteSpace(product.SdsFilePath))
        {
            ShowError("Ficha de Segurança não disponível para esta matéria-prima.");
            return;
        }

        try
        {
            HasError = false;
            if (!File.Exists(product.SdsFilePath))
            {
                ShowError("O ficheiro original da SDS foi movido, apagado ou está inacessível no sistema.");
                return;
            }

            IsLoadingPreview = true;
            IsPdfPreviewVisible = true;
            PdfPreviewImages.Clear();

            var imagePaths = await _pdfPreviewService.RenderPdfToImagesAsync(product.SdsFilePath);
            foreach (var path in imagePaths)
            {
                PdfPreviewImages.Add(path);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao tentar abrir o PDF: {ex.Message}");
            IsPdfPreviewVisible = false;
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    [RelayCommand]
    private void ClosePreview()
    {
        IsPdfPreviewVisible = false;
        PdfPreviewImages.Clear();
    }

    [RelayCommand]
    public async Task DeleteProductAsync(SupplierProduct product)
    {
        if (product == null) return;

        try
        {
            var window = (Microsoft.UI.Xaml.Application.Current as NanopaintHub_WinUIClient.App)?.MainWindow;
            if (window != null)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Confirmar Exclusão",
                    Content = $"Tem a certeza que deseja excluir a matéria-prima '{product.Name}'? Esta ação não pode ser desfeita.",
                    PrimaryButtonText = "Excluir",
                    CloseButtonText = "Cancelar",
                    DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
                    XamlRoot = window.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    return;
                }
            }

            HasError = false;
            await _supplierProductRepository.DeleteAsync(product.Id);
            await _unitOfWork.CommitAsync();
            
            SupplierProducts.Remove(product);
            ApplyGrouping();
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Ocorreu um erro ao tentar excluir: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    [RelayCommand]
    public void ToggleViewMode()
    {
        IsCardView = !IsCardView;
    }

    [RelayCommand]
    private void CreateProduct()
    {
        _navigationService.NavigateTo("SupplierProductEditor");
    }

    [RelayCommand]
    private void EditProduct(SupplierProduct? product)
    {
        if (product != null)
        {
            _navigationService.NavigateTo("SupplierProductEditor", product.Id);
        }
    }

    private void ApplyGrouping()
    {
        var groups = SupplierProducts
            .GroupBy(p => p.Supplier?.Name ?? "Sem Fornecedor")
            .OrderBy(g => g.Key)
            .Select(g => new SupplierGroup(g.Key, g.OrderBy(p => p.Name).ToList()))
            .ToList();

        GroupedSupplierProducts.Clear();
        foreach (var g in groups)
        {
            GroupedSupplierProducts.Add(g);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private void ApplySupplierSearch()
    {
        if (string.IsNullOrWhiteSpace(SupplierSearchQuery))
        {
            FilteredSuppliers = new ObservableCollection<Supplier>(_allSuppliers);
            return;
        }

        var query = SupplierSearchQuery.Trim().ToLower();
        var filtered = _allSuppliers
            .Where(s => (s.Name != null && s.Name.ToLower().Contains(query)) ||
                        (s.Country != null && s.Country.ToLower().Contains(query)) ||
                        (s.TechnicalContact != null && s.TechnicalContact.ToLower().Contains(query)))
            .ToList();

        FilteredSuppliers = new ObservableCollection<Supplier>(filtered);
    }

    [RelayCommand]
    private async Task CreateSupplierAsync()
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
                if (_allSuppliers.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowError($"Já existe um fornecedor registado com o nome '{name}'.");
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

                // Reload data to ensure products collection is fully set
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao criar fornecedor: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EditSupplierAsync(Supplier supplier)
    {
        if (supplier == null) return;

        var window = (Microsoft.UI.Xaml.Application.Current as NanopaintHub_WinUIClient.App)?.MainWindow;
        if (window == null) return;

        var nameInput = new Microsoft.UI.Xaml.Controls.TextBox { Header = "Nome do Fornecedor *", Text = supplier.Name, PlaceholderText = "Ex: BASF" };
        var countryInput = new Microsoft.UI.Xaml.Controls.TextBox { Header = "País", Text = supplier.Country, PlaceholderText = "Ex: Alemanha", Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0) };
        var contactInput = new Microsoft.UI.Xaml.Controls.TextBox { Header = "Contacto Técnico", Text = supplier.TechnicalContact, PlaceholderText = "Ex: sds-support@basf.com", Margin = new Microsoft.UI.Xaml.Thickness(0, 12, 0, 0) };

        var panel = new Microsoft.UI.Xaml.Controls.StackPanel();
        panel.Children.Add(nameInput);
        panel.Children.Add(countryInput);
        panel.Children.Add(contactInput);

        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
        {
            Title = "Editar Fornecedor",
            Content = panel,
            PrimaryButtonText = "Guardar",
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
                
                // Check duplicates if name changed
                if (!supplier.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                    _allSuppliers.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    ShowError($"Já existe um fornecedor registado com o nome '{name}'.");
                    return;
                }

                supplier.Name = name;
                supplier.Country = countryInput.Text.Trim();
                supplier.TechnicalContact = contactInput.Text.Trim();

                await _supplierRepository.UpdateAsync(supplier);
                await _unitOfWork.CommitAsync();

                // Reload data
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao editar fornecedor: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync(Supplier supplier)
    {
        if (supplier == null) return;

        // Check if supplier has associated products (raw materials)
        if (supplier.Products != null && supplier.Products.Any())
        {
            ShowError($"Não é possível excluir o fornecedor '{supplier.Name}' porque ele possui {supplier.Products.Count} matéria(s)-prima(s) associada(s).");
            return;
        }

        var window = (Microsoft.UI.Xaml.Application.Current as NanopaintHub_WinUIClient.App)?.MainWindow;
        if (window != null)
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Confirmar Exclusão",
                Content = $"Tem a certeza que deseja excluir o fornecedor '{supplier.Name}'? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Excluir",
                CloseButtonText = "Cancelar",
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
                XamlRoot = window.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return;
            }
        }

        try
        {
            await _supplierRepository.DeleteAsync(supplier.Id);
            await _unitOfWork.CommitAsync();

            // Reload data
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao excluir fornecedor: {ex.Message}");
        }
    }
}
