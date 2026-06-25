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
    private readonly IChromiumTinyMceEditorService _editorService;
    private readonly ITdsHtmlGenerator _htmlGenerator;
    private readonly IPdfLayoutService _pdfLayoutService;

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

    public TemplateEditorViewModel(
        ITemplateRepository templateRepository, 
        INavigationService navigationService, 
        IChromiumTinyMceEditorService editorService,
        ITdsHtmlGenerator htmlGenerator,
        IPdfLayoutService pdfLayoutService)
    {
        _templateRepository = templateRepository;
        _navigationService = navigationService;
        _editorService = editorService;
        _htmlGenerator = htmlGenerator;
        _pdfLayoutService = pdfLayoutService;
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

        bool hasLoaded = false;
        if (!string.IsNullOrWhiteSpace(Template.BuilderJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                BuilderModel = JsonSerializer.Deserialize<TemplateBuilderModel>(Template.BuilderJson, options) ?? new TemplateBuilderModel();
                if (BuilderModel.Sections != null && BuilderModel.Sections.Count > 0)
                {
                    hasLoaded = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TemplateEditorViewModel] JSON deserialize falhou: {ex.Message}");
            }
        }

        if (!hasLoaded && (!string.IsNullOrWhiteSpace(Template.HtmlStructure) || !string.IsNullOrWhiteSpace(Template.CssStructure)))
        {
            BuilderModel = TemplateHtmlParser.Parse(Template.HtmlStructure, Template.CssStructure);
        }
        else if (!hasLoaded)
        {
            BuilderModel = new TemplateBuilderModel();
        }

        if (BuilderModel.Layout != null)
        {
            BuilderModel.Layout.TopMargin = Template.TopMargin;
            BuilderModel.Layout.BottomMargin = Template.BottomMargin;
        }
    }

    public void SyncBuilderToHtml()
    {
        if (Template == null) return;

        // Map UI model to Application schema
        var schema = TemplateHtmlParser.MapModelToSchema(BuilderModel);

        // Normalize layout (enforce safety margins Top >= 45, Bottom >= 40 if headers/footers repeats are on)
        _pdfLayoutService.NormalizeLayout(schema, Template);

        // Sync margins to Template entity
        Template.TopMargin = (int)schema.Layout.TopMargin;
        Template.BottomMargin = (int)schema.Layout.BottomMargin;

        // Sync normalized values back to BuilderModel UI properties
        if (BuilderModel.Layout != null)
        {
            BuilderModel.Layout.TopMargin = schema.Layout.TopMargin;
            BuilderModel.Layout.BottomMargin = schema.Layout.BottomMargin;
            BuilderModel.Layout.LeftMargin = schema.Layout.LeftMargin;
            BuilderModel.Layout.RightMargin = schema.Layout.RightMargin;
        }

        // Serialize Builder UI state
        Template.BuilderJson = JsonSerializer.Serialize(BuilderModel);
        
        // Generate structured HTML and CSS using the application service
        Template.HtmlStructure = _htmlGenerator.GenerateHtml(schema);
        Template.CssStructure = _htmlGenerator.GenerateCss(schema);
        
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
            // Only synchronize from Builder to HTML if the user hasn't explicitly customized the HTML via TinyMCE.
            if (!IsCustomizedHtml)
            {
                SyncBuilderToHtml();
            }

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
        BuilderModel.Sections.Add(new TemplateSection { Title = "Sensor specifications", Type = "Table", DataSource = "Tds", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", Content = "" });
        BuilderModel.Sections.Add(new TemplateSection { Title = "Performance charts", Type = "Charts", DataSource = "Charts", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145" });
        SyncBuilderToHtml();
        OnPropertyChanged(nameof(BuilderModel));
    }

    [RelayCommand]
    private async Task EditGlobalHtmlAsync()
    {
        if (Template == null) return;

        // Sync changes made in the UI into HtmlStructure before opening Editor
        SyncBuilderToHtml();

        var assetsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
        var result = await _editorService.OpenEditorAsync(
            Template.HtmlStructure, 
            assetsPath, 
            Template.CssStructure,
            ChromiumTemplateEditorMode.WordLike);

        if (result.Applied && !string.IsNullOrWhiteSpace(result.Html))
        {
            Template.HtmlStructure = result.Html;
            IsCustomizedHtml = true;
            
            // WE NO LONGER DO SyncHtmlToBuilder() HERE!
            // Reverse-parsing complex TinyMCE edits back to the strict UI builder model causes data loss.
            // By setting IsCustomizedHtml = true, we lock the UI and rely on the saved HTML directly.

            OnPropertyChanged(nameof(Template));
            OnPropertyChanged(nameof(BuilderModel));
        }
    }

    [RelayCommand]
    private async Task EditSectionContentAsync(TemplateSection section)
    {
        if (section == null || Template == null) return;

        // Sync latest CSS to pass to editor
        SyncBuilderToHtml();

        var assetsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
        var result = await _editorService.OpenEditorAsync(
            section.Content, 
            assetsPath, 
            Template.CssStructure,
            ChromiumTemplateEditorMode.WordLike);

        if (result.Applied && !string.IsNullOrWhiteSpace(result.Html))
        {
            section.Content = result.Html;
            SyncBuilderToHtml();
        }
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
