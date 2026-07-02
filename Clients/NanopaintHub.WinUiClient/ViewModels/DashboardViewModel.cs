namespace NanopaintHub_WinUIClient.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using System.Linq;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IProductRepository _productRepository;
    private readonly IDocumentReleaseRepository _documentReleaseRepository;

    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _totalDocumentsGenerated;

    [ObservableProperty]
    private int _outdatedDocuments;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<DocumentStatItem> RecentDocuments { get; } = new();

    public DashboardViewModel(
        IProductRepository productRepository,
        IDocumentReleaseRepository documentReleaseRepository)
    {
        _productRepository = productRepository;
        _documentReleaseRepository = documentReleaseRepository;
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var products = await _productRepository.GetAllAsync();
            TotalProducts = products.Count();

            var releases = await _documentReleaseRepository.GetAllAsync();
            TotalDocumentsGenerated = releases.Count();

            // Example business rule: generated more than 6 months ago might need review (REACH compliance usually implies this)
            var outdatedThreshold = DateTime.UtcNow.AddMonths(-6);
            OutdatedDocuments = releases.Count(r => r.GeneratedAt < outdatedThreshold);

            RecentDocuments.Clear();
            var recent = releases.OrderByDescending(r => r.GeneratedAt).Take(10);
            foreach (var r in recent)
            {
                RecentDocuments.Add(new DocumentStatItem 
                {
                    ProductName = r.ProductName,
                    DocumentType = r.DocumentType,
                    GeneratedAt = r.GeneratedAt,
                    Version = r.Version
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class DocumentStatItem
{
    public string ProductName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public string DisplayDate => GeneratedAt.ToLocalTime().ToString("g");
}
