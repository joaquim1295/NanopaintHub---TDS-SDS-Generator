namespace NanopaintHub_WinUIClient.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

public partial class SupplierProductsViewModel : ObservableObject
{
    private readonly ISupplierProductRepository _repository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IEchaSubstanceRepository _echaRepository;
    private readonly ISdsPdfExtractorService _pdfExtractorService;

    [ObservableProperty]
    private ObservableCollection<SupplierProduct> _supplierProducts = new();

    public SupplierProductsViewModel(
        ISupplierProductRepository repository, 
        ISupplierRepository supplierRepository,
        IEchaSubstanceRepository echaRepository,
        ISdsPdfExtractorService pdfExtractorService)
    {
        _repository = repository;
        _supplierRepository = supplierRepository;
        _echaRepository = echaRepository;
        _pdfExtractorService = pdfExtractorService;
    }

    public async Task InitializeAsync()
    {
        var products = await _repository.SearchByNameAsync("");
        SupplierProducts.Clear();
        foreach (var product in products)
        {
            SupplierProducts.Add(product);
        }
    }
}
