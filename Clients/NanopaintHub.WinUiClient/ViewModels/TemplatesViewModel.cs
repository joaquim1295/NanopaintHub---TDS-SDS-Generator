using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class TemplatesViewModel : ObservableObject
{
    private readonly ITemplateRepository _templateRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<DocumentTemplate> _templates = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCardView = true;

    [ObservableProperty]
    private ObservableCollection<DocumentTemplate> _filteredTemplates = new();

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

    public TemplatesViewModel(ITemplateRepository templateRepository, INavigationService navigationService)
    {
        _templateRepository = templateRepository;
        _navigationService = navigationService;
        _isCardView = _isGlobalCardView;
    }

    [RelayCommand]
    private async Task LoadTemplatesAsync()
    {
        IsLoading = true;
        try
        {
            var data = await _templateRepository.GetAllAsync();
            Templates.Clear();
            foreach (var item in data)
            {
                Templates.Add(item);
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
        FilteredTemplates.Clear();
        var lowerSearch = SearchText?.ToLowerInvariant() ?? string.Empty;
        foreach (var p in Templates)
        {
            if (string.IsNullOrWhiteSpace(lowerSearch) || 
                (p.TemplateName != null && p.TemplateName.ToLowerInvariant().Contains(lowerSearch)) || 
                (p.DocumentType != null && p.DocumentType.ToLowerInvariant().Contains(lowerSearch)))
            {
                FilteredTemplates.Add(p);
            }
        }
    }

    [RelayCommand]
    private void CreateTemplate()
    {
        _navigationService.NavigateTo("TemplateEditor");
    }

    [RelayCommand]
    private void EditTemplate(DocumentTemplate? template)
    {
        if (template != null)
        {
            _navigationService.NavigateTo("TemplateEditor", template.Id);
        }
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(DocumentTemplate? template)
    {
        if (template != null)
        {
            await _templateRepository.DeleteAsync(template.Id);
            Templates.Remove(template);
            ApplyFilter();
        }
    }
}
