using System;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Models;
using NanopaintHub_WinUIClient.Services;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class TemplateEditorViewModel : ObservableObject
{
    private readonly ITemplateRepository _templateRepository;
    private readonly INavigationService _navigationService;

    public List<string> HeaderRepeatStyles { get; } = new() { "None", "Minimal", "Original" };
    public List<string> FooterRepeatStyles { get; } = new() { "None", "Original" };
    public List<string> AvailableFonts { get; } = new() { "Segoe UI", "Arial", "Helvetica", "Times New Roman", "Courier New", "Verdana", "Georgia", "Tahoma", "Trebuchet MS", "Outfit", "Inter" };

    [ObservableProperty]
    private DocumentTemplate? _template;

    [ObservableProperty]
    private TemplateBuilderModel _builderModel = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCustomizedHtml;

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
                
                LoadBuilderModel();
            }
            else
            {
                Template = new DocumentTemplate 
                { 
                    TemplateName = "",
                    DocumentType = "TDS",
                    HtmlStructure = "",
                    CssStructure = ""
                };
                BuilderModel = new TemplateBuilderModel();
                PageTitle = "Novo Template";
                GenerateDefaultTdsLayout();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadBuilderModel()
    {
        if (Template == null) return;

        if (!string.IsNullOrWhiteSpace(Template.BuilderJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                BuilderModel = JsonSerializer.Deserialize<TemplateBuilderModel>(Template.BuilderJson, options) ?? new TemplateBuilderModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TemplateEditorViewModel] JSON deserialize falhou: {ex.Message}");
                // Fallback se JSON for inválido
                BuilderModel = TemplateHtmlParser.Parse(Template.HtmlStructure, Template.CssStructure);
            }
        }
        else if (!string.IsNullOrWhiteSpace(Template.HtmlStructure) || !string.IsNullOrWhiteSpace(Template.CssStructure))
        {
            // Reverse parsing do HTML antigo para blocos
            BuilderModel = TemplateHtmlParser.Parse(Template.HtmlStructure, Template.CssStructure);
        }
        else
        {
            BuilderModel = new TemplateBuilderModel();
        }
    }

    public void SyncBuilderToHtml()
    {
        if (Template == null) return;

        Template.BuilderJson = JsonSerializer.Serialize(BuilderModel);
        
        if (!IsCustomizedHtml)
        {
            Template.HtmlStructure = TdsHtmlGenerator.GenerateHtml(BuilderModel);
            Template.CssStructure = TdsHtmlGenerator.GenerateCss(BuilderModel);
        }
        
        OnPropertyChanged(nameof(Template)); // Refresh bind
    }

    public void SyncHtmlToBuilder()
    {
        if (Template == null) return;
        BuilderModel = TemplateHtmlParser.Parse(Template.HtmlStructure, Template.CssStructure);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Template == null) return;

        IsLoading = true;
        try
        {
            SyncBuilderToHtml();

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

    [RelayCommand]
    private void GenerateDefaultTdsLayout()
    {
        if (Template == null) return;

        BuilderModel.Theme.FontFamily = "Segoe UI";
        BuilderModel.Theme.PrimaryColor = "#0f2a4a";
        BuilderModel.Theme.SecondaryColor = "#19528d";

        BuilderModel.Header.FontFamily = "Segoe UI";
        BuilderModel.Header.TextColor = "#ffffff";

        BuilderModel.Footer.FontFamily = "Segoe UI";
        BuilderModel.Footer.TextColor = "#0f2a4a";

        BuilderModel.Sections.Clear();
        BuilderModel.Sections.Add(new TemplateSection { Title = "Ink features", Type = "BulletPoints", DataSource = "Features", IsHalfWidth = true, FontFamily = "Segoe UI", TextColor = "#223145" });
        BuilderModel.Sections.Add(new TemplateSection { Title = "Ink properties", Type = "Table", DataSource = "LiquidProperties", IsHalfWidth = true, FontFamily = "Segoe UI", TextColor = "#223145" });
        BuilderModel.Sections.Add(new TemplateSection { Title = "Handling Guidelines", Type = "Table", DataSource = "MechanicalProperties", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145" });
        BuilderModel.Sections.Add(new TemplateSection { Title = "Sensor specifications", Type = "Table", DataSource = "Tds", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145" });
        BuilderModel.Sections.Add(new TemplateSection { Title = "Performance charts", Type = "Charts", DataSource = "Charts", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145" });
        SyncBuilderToHtml();
        OnPropertyChanged(nameof(BuilderModel));
    }

    [RelayCommand]
    private void AddSection()
    {
        BuilderModel.Sections.Add(new TemplateSection());
    }

    [RelayCommand]
    private void RemoveSection(TemplateSection section)
    {
        if (section != null && BuilderModel.Sections.Contains(section))
        {
            BuilderModel.Sections.Remove(section);
        }
    }
}
