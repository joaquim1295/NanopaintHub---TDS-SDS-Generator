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

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCardView = true;

    public ProductsViewModel(IProductRepository productRepository, INavigationService navigationService)
    {
        _productRepository = productRepository;
        _navigationService = navigationService;
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
            await _productRepository.DeleteAsync(product.Id);
            Products.Remove(product);
        }
    }
}
