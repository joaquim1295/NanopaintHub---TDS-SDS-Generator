using System;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using NanopaintHub_WinUIClient.Models;
using NanopaintHub_WinUIClient.Services;
using Microsoft.Extensions.Configuration;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class TemplateEditorViewModel : ObservableObject
{
    private readonly ITemplateRepository _templateRepository;
    private readonly INavigationService _navigationService;
    private readonly IChromiumTinyMceEditorService _editorService;
    private readonly ITdsHtmlGenerator _htmlGenerator;
    private readonly IPdfLayoutService _pdfLayoutService;
    private readonly IDocumentGeneratorService _documentGeneratorRouter;
    private readonly PdfPreviewService _pdfPreviewService;

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
    private ObservableCollection<BitmapImage> _pdfPreviewImages = new();

    [ObservableProperty]
    private bool _isPdfPreviewVisible;

    [ObservableProperty]
    private bool _isLoadingPreview;

    [ObservableProperty]
    private string _pageTitle = "Novo Template";

    [ObservableProperty]
    private bool _isQuestPdfSelected;

    [ObservableProperty]
    private bool _isNotQuestPdfSelected;

    public TemplateEditorViewModel(
        ITemplateRepository templateRepository, 
        INavigationService navigationService, 
        IChromiumTinyMceEditorService editorService,
        ITdsHtmlGenerator htmlGenerator,
        IPdfLayoutService pdfLayoutService,
        IDocumentGeneratorService documentGeneratorRouter,
        PdfPreviewService pdfPreviewService,
        IConfiguration configuration)
    {
        _templateRepository = templateRepository;
        _navigationService = navigationService;
        _editorService = editorService;
        _htmlGenerator = htmlGenerator;
        _pdfLayoutService = pdfLayoutService;
        _documentGeneratorRouter = documentGeneratorRouter;
        _pdfPreviewService = pdfPreviewService;

        var engine = configuration["PdfEngine"] ?? "questpdf";
        IsQuestPdfSelected = string.Equals(engine, "questpdf", StringComparison.OrdinalIgnoreCase);
        IsNotQuestPdfSelected = !IsQuestPdfSelected;
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
    public async Task GeneratePreviewPdfAsync()
    {
        if (Template == null) return;
        
        IsLoadingPreview = true;
        try
        {
            if (!IsCustomizedHtml)
            {
                SyncBuilderToHtml();
            }

            string loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam in odio in metus consequat aliquet. Phasellus eget nunc sit amet metus aliquet varius. Sed et felis et ante luctus dignissim vel ac ex. Vivamus varius ex id velit vulputate, sed consequat tellus pulvinar. Maecenas bibendum magna nisi, non tristique nisl mattis vitae. Cras in nulla quis justo tincidunt faucibus eu nec mi. Vestibulum tristique sapien vitae dui vulputate volutpat.\n\nPhasellus id fermentum metus, eget euismod nunc. Quisque id ligula nisl. In eleifend odio nisi, id condimentum orci scelerisque id. Curabitur vel leo id urna hendrerit aliquet vel et mi. Nunc lacinia metus id est gravida fermentum. Mauris egestas magna lectus, a sagittis risus sollicitudin imperdiet. Nullam tempor massa id arcu cursus porttitor.";

            var dummyProduct = new Product
            {
                Name = "Produto Exemplo Preview",
                Code = "PRV-001",
                Description = loremIpsum,
                RevisionDate = DateTime.Today,
                DocumentVersion = 1,
                FeaturesJson = "[\"Resistência alta (Lorem ipsum dolor sit amet)\", \"Flexível (Consectetur adipiscing elit)\", \"Durável (Phasellus eget nunc sit amet metus aliquet)\", \"Ecológico (Sed et felis et ante luctus dignissim)\"]",
                LiquidPropertiesJson = $"{{\"Viscosidade\":\"100 cP\", \"Densidade\":\"1.2 g/cm³\", \"Sólidos\":\"45%\", \"Ph\":\"7.2\"}}",
                MechanicalPropertiesJson = $"{{\"Dureza\":\"60 Shore A\", \"Alongamento\":\"300%\", \"Tração\":\"5 MPa\"}}",
                FunctionalPropertiesJson = $"{{\"Resistência\":\"1 kOhm\", \"Sensibilidade\":\"0.87 V/N\"}}",
                TdsSensorSpecsJson = $"{{\"ForceUnit\":\"N\", \"SensitivityVolts\":0.87, \"SensitivityPerUnit\":0.082, \"ResponseTimeSec\":0.018, \"RecoveryTimeSec\":0.025, \"RepeatabilityPercent\":2.1, \"RestResistanceOhms\":118000, \"HysteresisPercent\":4.2, \"OperatingTempMinC\":-10, \"OperatingTempMaxC\":85, \"RangeMin\":0, \"RangeMax\":50}}"
            };

            var tempTemplate = new DocumentTemplate
            {
                DocumentType = Template.DocumentType,
                TemplateName = Template.TemplateName,
                TopMargin = Template.TopMargin,
                BottomMargin = Template.BottomMargin,
                HtmlStructure = Template.HtmlStructure ?? string.Empty,
                CssStructure = Template.CssStructure ?? string.Empty,
                BuilderJson = Template.BuilderJson
            };

            byte[] pdfBytes;
            if (tempTemplate.DocumentType == "SDS")
            {
                pdfBytes = await _documentGeneratorRouter.GenerateSdsAsync(dummyProduct, tempTemplate);
            }
            else
            {
                pdfBytes = await _documentGeneratorRouter.GenerateTdsAsync(dummyProduct, tempTemplate, null);
            }
            
            var tempPdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            await System.IO.File.WriteAllBytesAsync(tempPdfPath, pdfBytes);
            
            var imagePaths = await _pdfPreviewService.RenderPdfToImagesAsync(tempPdfPath);
            PdfPreviewImages.Clear();
            foreach (var imgPath in imagePaths)
            {
                var bitmap = new BitmapImage(new Uri(imgPath));
                PdfPreviewImages.Add(bitmap);
            }
            
            IsPdfPreviewVisible = true;
        }
        catch (Exception ex)
        {
            // Omit error handling for now or handle as needed
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    [RelayCommand]
    private void ClosePreview()
    {
        IsPdfPreviewVisible = false;
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
        var window = new Views.TinyMceEditorWindow(
            Template.HtmlStructure,
            assetsPath,
            Template.CssStructure);

        window.Activate();
        
        var resultHtml = await window.WaitForResultAsync();

        if (resultHtml != null && !string.IsNullOrWhiteSpace(resultHtml))
        {
            Template.HtmlStructure = resultHtml;
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
        var window = new Views.TinyMceEditorWindow(
            section.Content,
            assetsPath,
            Template.CssStructure);

        window.Activate();

        var resultHtml = await window.WaitForResultAsync();

        if (resultHtml != null && !string.IsNullOrWhiteSpace(resultHtml))
        {
            section.Content = resultHtml;
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
