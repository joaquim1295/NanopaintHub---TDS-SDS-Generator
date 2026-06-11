using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class TemplateEditorViewModel : ObservableObject
{
    private readonly ITemplateRepository _templateRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private DocumentTemplate? _template;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "Novo Template";

    public TemplateEditorViewModel(ITemplateRepository templateRepository, INavigationService navigationService)
    {
        _templateRepository = templateRepository;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync(Guid? templateId)
    {
        IsLoading = true;
        try
        {
            if (templateId.HasValue && templateId.Value != Guid.Empty)
            {
                Template = await _templateRepository.GetByIdAsync(templateId.Value);
                PageTitle = $"Editar Template - {Template?.TemplateName}";
            }
            else
            {
                Template = new DocumentTemplate 
                { 
                    TemplateName = "",
                    DocumentType = "TDS",
                    HtmlStructure = "<p>Digite o template aqui...</p>"
                };
                PageTitle = "Novo Template";
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
        if (Template == null) return;

        IsLoading = true;
        try
        {
            var existing = await _templateRepository.GetByIdAsync(Template.Id);
            if (existing != null)
            {
                await _templateRepository.UpdateAsync(Template);
            }
            else
            {
                await _templateRepository.AddAsync(Template);
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
