using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class ProductEditorViewModel : ObservableObject
{
    private readonly IProductRepository _productRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Product? _product;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "Novo Produto";

    public ProductEditorViewModel(IProductRepository productRepository, INavigationService navigationService)
    {
        _productRepository = productRepository;
        _navigationService = navigationService;
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
                    TdsSensorSpecsJson = "{}"
                };
                PageTitle = "Novo Produto";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Product == null) return;

        IsLoading = true;
        try
        {
            var existing = await _productRepository.GetByIdAsync(Product.Id);
            if (existing != null)
            {
                await _productRepository.UpdateAsync(Product);
            }
            else
            {
                await _productRepository.AddAsync(Product);
            }

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
}
