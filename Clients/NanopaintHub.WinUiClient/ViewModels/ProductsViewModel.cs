using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductRepository _productRepository;
    private readonly INavigationService _navigationService;
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCardView = true;

    [ObservableProperty]
    private ObservableCollection<Product> _filteredProducts = new();

    private static bool _isGlobalCardView = true;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public ProductsViewModel(IProductRepository productRepository, INavigationService navigationService, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _navigationService = navigationService;
        _unitOfWork = unitOfWork;
        _isCardView = _isGlobalCardView;
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            var data = await _productRepository.GetAllAsync();
            Products.Clear();
            foreach (var item in data)
            {
                Products.Add(item);
            }
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        IsCardView = !IsCardView;
        _isGlobalCardView = IsCardView;
    }

    private void ApplyFilter()
    {
        FilteredProducts.Clear();
        var lowerSearch = SearchText?.ToLowerInvariant() ?? string.Empty;
        foreach (var p in Products)
        {
            if (string.IsNullOrWhiteSpace(lowerSearch) || 
                (p.Name != null && p.Name.ToLowerInvariant().Contains(lowerSearch)) || 
                (p.Code != null && p.Code.ToLowerInvariant().Contains(lowerSearch)))
            {
                FilteredProducts.Add(p);
            }
        }
    }

    [RelayCommand]
    private void CreateProduct()
    {
        _navigationService.NavigateTo("ProductEditor");
    }

    [RelayCommand]
    private void EditProduct(Product? product)
    {
        if (product != null)
        {
            _navigationService.NavigateTo("ProductEditor", product.Id);
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync(Product? product)
    {
        if (product != null)
        {
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
            if (window != null)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "Confirmar Exclusão",
                    Content = $"Tem a certeza que deseja excluir o produto '{product.Name}'? Esta ação não pode ser desfeita.",
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

            await _productRepository.DeleteAsync(product.Id);
            await _unitOfWork.CommitAsync();
            Products.Remove(product);
            ApplyFilter();
        }
    }
}
