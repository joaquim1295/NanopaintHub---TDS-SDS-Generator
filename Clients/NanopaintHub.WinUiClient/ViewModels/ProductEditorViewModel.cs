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

    [ObservableProperty]
    private TdsSensorSpecs _sensorSpecs = new();

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

        Product.TdsSensorSpecsJson = JsonSerializer.Serialize(SensorSpecs);
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
    private async Task SaveAsync()
    {
        if (Product == null) return;

        SaveCollectionsToProduct();

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
