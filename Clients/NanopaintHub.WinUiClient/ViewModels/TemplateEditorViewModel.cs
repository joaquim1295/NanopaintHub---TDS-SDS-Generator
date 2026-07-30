using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub_WinUIClient.Services;
using NanopaintHub_WinUIClient.Models;
using Microsoft.Extensions.Configuration;
using Windows.Storage.Streams;
using System.IO;

namespace NanopaintHub_WinUIClient.ViewModels;

public class SdsOfficialSection
{
    public string Title { get; set; }
    public string Content { get; set; }
}

public partial class TemplateEditorViewModel : ObservableObject
{
    private readonly ITemplateRepository _templateRepository;
    private readonly INavigationService _navigationService;
    private readonly IChromiumTinyMceEditorService _editorService;
    private readonly ITdsHtmlGenerator _htmlGenerator;
    private readonly IPdfLayoutService _pdfLayoutService;
    private readonly IDocumentGeneratorService _documentGeneratorRouter;
    private readonly PdfPreviewService _pdfPreviewService;

    private CancellationTokenSource? _previewCts;
    
    [ObservableProperty]
    private ObservableCollection<BitmapImage> _pdfPreviewImages = new();

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
    private bool _isPdfPreviewVisible;

    [ObservableProperty]
    private bool _isLoadingPreview;

    private readonly IConfiguration _configuration;

    [ObservableProperty]
    private string _pageTitle = "Novo Template";

    [ObservableProperty]
    private bool _isQuestPdfSelected;

    [ObservableProperty]
    private bool _isNotQuestPdfSelected;

    [ObservableProperty]
    private bool _isSdsSelected;

    [ObservableProperty]
    private bool _isTdsSelected = true;

    [ObservableProperty]
    private string _companyName;

    [ObservableProperty]
    private string _companyAddress;

    [ObservableProperty]
    private string _companyTelephone;

    [ObservableProperty]
    private string _companyEmergencyTelephone;

    [ObservableProperty]
    private string _companyEmail;

    [ObservableProperty]
    private string _companyResponsiblePerson;

    private readonly string _appSettingsPath;

    public ObservableCollection<SdsOfficialSection> SdsOfficialSections { get; } = new()
    {
        new SdsOfficialSection { Title = "SECTION 1: Identification of the substance/mixture and of the company/undertaking", Content = "1.1. Product identifier\n1.2. Relevant identified uses of the substance or mixture and uses advised against\n1.3. Details of the supplier of the safety data sheet\n1.4. Emergency telephone number" },
        new SdsOfficialSection { Title = "SECTION 2: Hazards identification", Content = "2.1. Classification of the substance or mixture\n2.2. Label elements\n2.3. Other hazards" },
        new SdsOfficialSection { Title = "SECTION 3: Composition/information on ingredients", Content = "3.1. Substances\n3.2. Mixtures" },
        new SdsOfficialSection { Title = "SECTION 4: First aid measures", Content = "4.1. Description of first aid measures\n4.2. Most important symptoms and effects, both acute and delayed\n4.3. Indication of any immediate medical attention and special treatment needed" },
        new SdsOfficialSection { Title = "SECTION 5: Firefighting measures", Content = "5.1. Extinguishing media\n5.2. Special hazards arising from the substance or mixture\n5.3. Advice for firefighters" },
        new SdsOfficialSection { Title = "SECTION 6: Accidental release measures", Content = "6.1. Personal precautions, protective equipment and emergency procedures\n6.2. Environmental precautions\n6.3. Methods and material for containment and cleaning up\n6.4. Reference to other sections" },
        new SdsOfficialSection { Title = "SECTION 7: Handling and storage", Content = "7.1. Precautions for safe handling\n7.2. Conditions for safe storage, including any incompatibilities\n7.3. Specific end use(s)" },
        new SdsOfficialSection { Title = "SECTION 8: Exposure controls/personal protection", Content = "8.1. Control parameters\n8.2. Exposure controls\nPersonal protective equipment (PPE) requirements." },
        new SdsOfficialSection { Title = "SECTION 9: Physical and chemical properties", Content = "9.1. Information on basic physical and chemical properties\n9.2. Other information (e.g. VOC content, specific gravity)." },
        new SdsOfficialSection { Title = "SECTION 10: Stability and reactivity", Content = "10.1. Reactivity\n10.2. Chemical stability\n10.3. Possibility of hazardous reactions\n10.4. Conditions to avoid\n10.5. Incompatible materials\n10.6. Hazardous decomposition products" },
        new SdsOfficialSection { Title = "SECTION 11: Toxicological information", Content = "11.1. Information on toxicological effects\nAcute toxicity, skin corrosion/irritation, serious eye damage/irritation, respiratory or skin sensitisation, germ cell mutagenicity, carcinogenicity, reproductive toxicity, STOT-single exposure, STOT-repeated exposure, aspiration hazard." },
        new SdsOfficialSection { Title = "SECTION 12: Ecological information", Content = "12.1. Toxicity\n12.2. Persistence and degradability\n12.3. Bioaccumulative potential\n12.4. Mobility in soil\n12.5. Results of PBT and vPvB assessment\n12.6. Other adverse effects" },
        new SdsOfficialSection { Title = "SECTION 13: Disposal considerations", Content = "13.1. Waste treatment methods\nWaste codes, disposal of packaging, special precautions." },
        new SdsOfficialSection { Title = "SECTION 14: Transport information", Content = "14.1. UN number\n14.2. UN proper shipping name\n14.3. Transport hazard class(es)\n14.4. Packing group\n14.5. Environmental hazards\n14.6. Special precautions for user\n14.7. Transport in bulk according to Annex II of Marpol and the IBC Code" },
        new SdsOfficialSection { Title = "SECTION 15: Regulatory information", Content = "15.1. Safety, health and environmental regulations/legislation specific for the substance or mixture\n15.2. Chemical safety assessment" },
        new SdsOfficialSection { Title = "SECTION 16: Other information", Content = "Indication of changes, abbreviations and acronyms, key literature references, classification methods, list of relevant hazard statements." }
    };

    public void UpdateDocumentTypeFlags()
    {
        if (Template != null)
        {
            IsSdsSelected = Template.DocumentType == "SDS";
            IsTdsSelected = Template.DocumentType == "TDS";
            EvaluateQuestPdfEngine();
            
            if (BuilderModel?.Sections != null)
            {
                foreach (var section in BuilderModel.Sections)
                {
                    section.IsTdsContext = IsTdsSelected;
                    section.IsSdsContext = IsSdsSelected;
                }
            }
        }
    }

    
    // UNDO/REDO State
    private readonly System.Collections.Generic.Stack<string> _undoStack = new();
    private readonly System.Collections.Generic.Stack<string> _redoStack = new();
    private string? _lastSavedSnapshot;
    private bool _isRestoringState;
    private bool _skipNextSnapshot;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        
        _isRestoringState = true;
        _skipNextSnapshot = true;
        
        // Save current to redo
        if (_lastSavedSnapshot != null)
        {
            _redoStack.Push(_lastSavedSnapshot);
        }
        
        var previousStateJson = _undoStack.Pop();
        var previousState = System.Text.Json.JsonSerializer.Deserialize<NanopaintHub_WinUIClient.Models.TemplateBuilderModel>(previousStateJson);
        
        InjectState(previousState);
        _lastSavedSnapshot = previousStateJson;
        
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        
        _ = GeneratePreviewPdfAsync(); // Trigger preview for the restored state
        _isRestoringState = false;
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        
        _isRestoringState = true;
        _skipNextSnapshot = true;
        
        if (_lastSavedSnapshot != null)
        {
            _undoStack.Push(_lastSavedSnapshot);
        }
        
        var nextStateJson = _redoStack.Pop();
        var nextState = System.Text.Json.JsonSerializer.Deserialize<NanopaintHub_WinUIClient.Models.TemplateBuilderModel>(nextStateJson);
        
        InjectState(nextState);
        _lastSavedSnapshot = nextStateJson;
        
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        
        _ = GeneratePreviewPdfAsync();
        _isRestoringState = false;
    }
    
    private void SaveSnapshot()
    {
        if (_skipNextSnapshot)
        {
            _skipNextSnapshot = false;
            return;
        }

        if (_isRestoringState || BuilderModel == null) return;
        
        var currentJson = System.Text.Json.JsonSerializer.Serialize(BuilderModel);
        
        if (_lastSavedSnapshot == null)
        {
            _lastSavedSnapshot = currentJson;
            return;
        }
        
        if (currentJson != _lastSavedSnapshot)
        {
            _undoStack.Push(_lastSavedSnapshot);
            _lastSavedSnapshot = currentJson;
            _redoStack.Clear(); // Any new action invalidates Redo stack
            
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }
    }
    
    private void InjectState(NanopaintHub_WinUIClient.Models.TemplateBuilderModel source)
    {
        if (source == null || BuilderModel == null) return;
        
        CopyProperties(source.Theme, BuilderModel.Theme);
        CopyProperties(source.Layout, BuilderModel.Layout);
        CopyProperties(source.Header, BuilderModel.Header);
        CopyProperties(source.Footer, BuilderModel.Footer);
        CopyProperties(source.Batch, BuilderModel.Batch);
        
        BuilderModel.Sections.Clear();
        foreach (var sec in source.Sections)
        {
            var newSec = new NanopaintHub_WinUIClient.Models.TemplateSection();
            CopyProperties(sec, newSec);
            BuilderModel.Sections.Add(newSec);
        }
    }

    private void CopyProperties<T>(T source, T target)
    {
        if (source == null || target == null) return;
        foreach (var prop in typeof(T).GetProperties())
        {
            if (prop.CanRead && prop.CanWrite && prop.PropertyType != typeof(System.Collections.ObjectModel.ObservableCollection<NanopaintHub_WinUIClient.Models.TemplateSection>))
            {
                var val = prop.GetValue(source);
                // Avoid self-referential or unnecessary updates for ignored properties
                if (prop.Name == "HeaderBaseScalePercentage" || prop.Name == "SectionBaseScalePercentage") continue;
                prop.SetValue(target, val);
            }
        }
    }

    public TemplateEditorViewModel(
        ITemplateRepository templateRepository, 
        INavigationService navigationService, 
        IChromiumTinyMceEditorService editorService,
        ITdsHtmlGenerator htmlGenerator,
        IPdfLayoutService pdfLayoutService,
        IDocumentGeneratorService documentGeneratorRouter,
        PdfPreviewService pdfPreviewService,
        IConfiguration configuration,
        Microsoft.Extensions.Options.IOptionsMonitor<NanopaintHub.Domain.Common.CompanyInfoOptions> companyOptions)
    {
        _templateRepository = templateRepository;
        _navigationService = navigationService;
        _editorService = editorService;
        _htmlGenerator = htmlGenerator;
        _pdfLayoutService = pdfLayoutService;
        _documentGeneratorRouter = documentGeneratorRouter;
        _pdfPreviewService = pdfPreviewService;
        _configuration = configuration;

        var engine = configuration["PdfGenerator:TdsEngine"] ?? "questpdf";
        IsQuestPdfSelected = string.Equals(engine, "questpdf", StringComparison.OrdinalIgnoreCase);
        IsNotQuestPdfSelected = !IsQuestPdfSelected;

        CompanyName = !string.IsNullOrWhiteSpace(companyOptions.CurrentValue.Name) ? companyOptions.CurrentValue.Name : "Nanopaint, Lda";
        CompanyAddress = !string.IsNullOrWhiteSpace(companyOptions.CurrentValue.Address) ? companyOptions.CurrentValue.Address : "CIIES – Sala 23/28\nAvenida de Tibães, 1199\n4770-568 Vale de São Cosme, Portugal";
        CompanyTelephone = !string.IsNullOrWhiteSpace(companyOptions.CurrentValue.Telephone) ? companyOptions.CurrentValue.Telephone : "+351 253 601 681";
        CompanyEmergencyTelephone = !string.IsNullOrWhiteSpace(companyOptions.CurrentValue.EmergencyTelephone) ? companyOptions.CurrentValue.EmergencyTelephone : "+351 253 601 681";
        CompanyEmail = !string.IsNullOrWhiteSpace(companyOptions.CurrentValue.Email) ? companyOptions.CurrentValue.Email : "info@nanopaint-tech.com";
        CompanyResponsiblePerson = companyOptions.CurrentValue.ResponsiblePerson;

        var localFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub");
        _appSettingsPath = System.IO.Path.Combine(localFolder, "appsettings.user.json");
        
        SaveCompanySettingsAsync();
    }

    partial void OnCompanyNameChanged(string value) { SaveCompanySettingsAsync(); }
    partial void OnCompanyAddressChanged(string value) { SaveCompanySettingsAsync(); }
    partial void OnCompanyTelephoneChanged(string value) { SaveCompanySettingsAsync(); }
    partial void OnCompanyEmergencyTelephoneChanged(string value) { SaveCompanySettingsAsync(); }
    partial void OnCompanyEmailChanged(string value) { SaveCompanySettingsAsync(); }
    partial void OnCompanyResponsiblePersonChanged(string value) { SaveCompanySettingsAsync(); }

    private async void SaveCompanySettingsAsync()
    {
        try
        {
            var root = new System.Text.Json.Nodes.JsonObject();
            if (System.IO.File.Exists(_appSettingsPath))
            {
                var content = await System.IO.File.ReadAllTextAsync(_appSettingsPath);
                try { root = System.Text.Json.Nodes.JsonNode.Parse(content)?.AsObject() ?? new System.Text.Json.Nodes.JsonObject(); }
                catch { }
            }

            if (!root.ContainsKey("CompanyInfo"))
            {
                root["CompanyInfo"] = new System.Text.Json.Nodes.JsonObject();
            }
            var companyNode = root["CompanyInfo"]!.AsObject();
            companyNode["Name"] = CompanyName;
            companyNode["Address"] = CompanyAddress;
            companyNode["Telephone"] = CompanyTelephone;
            companyNode["EmergencyTelephone"] = CompanyEmergencyTelephone;
            companyNode["Email"] = CompanyEmail;
            companyNode["ResponsiblePerson"] = CompanyResponsiblePerson;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(_appSettingsPath, root.ToJsonString(options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving company settings: {ex.Message}");
        }
    }

    private void EvaluateQuestPdfEngine()
    {
        var engine = (Template?.DocumentType == "SDS") 
            ? (_configuration["PdfGenerator:SdsEngine"] ?? "questpdf")
            : (_configuration["PdfGenerator:TdsEngine"] ?? "questpdf");
            
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
                _lastSavedSnapshot = System.Text.Json.JsonSerializer.Serialize(BuilderModel);
        _undoStack.Clear();
        _redoStack.Clear();
PageTitle = "Novo Template";
                GenerateDefaultLayout();
            }
            UpdateDocumentTypeFlags();
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
        _lastSavedSnapshot = System.Text.Json.JsonSerializer.Serialize(BuilderModel);
        _undoStack.Clear();
        _redoStack.Clear();
}

        if (BuilderModel.Layout != null)
        {
            BuilderModel.Layout.TopMargin = Template.TopMargin;
            BuilderModel.Layout.BottomMargin = Template.BottomMargin;
        }
    }

        public void SyncBuilderToJson()
    {
        if (Template == null) return;
        Template.BuilderJson = JsonSerializer.Serialize(BuilderModel);
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
            BuilderModel.Layout.TopMargin = (int)schema.Layout.TopMargin;
            BuilderModel.Layout.BottomMargin = (int)schema.Layout.BottomMargin;
            BuilderModel.Layout.LeftMargin = (int)schema.Layout.LeftMargin;
            BuilderModel.Layout.RightMargin = (int)schema.Layout.RightMargin;
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
        
        if (Template.DocumentType == "SDS")
        {
            if (!string.IsNullOrWhiteSpace(Template.BuilderJson))
            {
                try
                {
                    BuilderModel = System.Text.Json.JsonSerializer.Deserialize<TemplateBuilderModel>(Template.BuilderJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TemplateBuilderModel();
            _lastSavedSnapshot = Template.BuilderJson;
            _undoStack.Clear();
            _redoStack.Clear();
                }
                catch
                {
                    BuilderModel = new TemplateBuilderModel();
                }
            }
            else
            {
                BuilderModel = new TemplateBuilderModel();
            }

            var hasCorruption = !BuilderModel.Sections.Any() || BuilderModel.Sections.Any(s => s.Type == "RichText");
            if (hasCorruption)
            {
                GenerateDefaultLayout();
            }
            return;
        }

        BuilderModel = TemplateHtmlParser.Parse(Template.HtmlStructure, Template.CssStructure);
    }

    [RelayCommand]
    public async Task GeneratePreviewPdfAsync()
    {
        if (Template == null) return;
        
        // Debounce Logic
        _previewCts?.Cancel();
        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        try
        {
            await Task.Delay(1500, token); // 1.5s debounce
        
        if (token.IsCancellationRequested) return;
        SaveSnapshot(); // Snapshot after debounce
        }
        catch (TaskCanceledException)
        {
            return; // Debounce interrupted
        }

        if (token.IsCancellationRequested) return;

        IsLoadingPreview = true;
        try
        {
            SyncBuilderToJson();

            string loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam in odio in metus consequat aliquet. Phasellus eget nunc sit amet metus aliquet varius. Sed et felis et ante luctus dignissim vel ac ex. Vivamus varius ex id velit vulputate, sed consequat tellus pulvinar.";

            var dummyProduct = new Product
            {
                Name = "TEST PRODUCT (PREVIEW)",
                Code = "PRV-001",
                Description = loremIpsum,
                ProductCategory = new ProductCategory { Name = "Example Category" },
                ProductType = "Ink Technology",
                RevisionDate = DateTime.Today,
                DocumentVersion = 1,
                FeaturesJson = "[\"High resistance (Lorem ipsum dolor sit amet)\", \"Flexible (Consectetur adipiscing elit)\", \"Durable (Phasellus eget nunc sit amet metus aliquet)\", \"Eco-friendly (Sed et felis et ante luctus dignissim)\"]",
                LiquidPropertiesJson = $"{{\"Viscosity\":\"100 cP\", \"Density\":\"1.2 g/cm³\", \"Solids\":\"45%\", \"Ph\":\"7.2\"}}",
                MechanicalPropertiesJson = $"{{\"Hardness\":\"60 Shore A\", \"Elongation\":\"300%\", \"Tensile strength\":\"5 MPa\"}}",
                FunctionalPropertiesJson = $"{{\"Resistance\":\"1 kOhm\", \"Sensitivity\":\"0.87 V/N\"}}",
                TdsSensorSpecsJson = $"{{\"ForceUnit\":\"N\", \"SensitivityVolts\":0.87, \"SensitivityPerUnit\":0.082, \"ResponseTimeSec\":0.018, \"RecoveryTimeSec\":0.025, \"RepeatabilityPercent\":2.1, \"RestResistanceOhms\":118000, \"HysteresisPercent\":4.2, \"OperatingTempMinC\":-10, \"OperatingTempMaxC\":85, \"RangeMin\":0, \"RangeMax\":50}}",
                Compositions = new List<ProductComposition>
                {
                    new ProductComposition
                    {
                        Percentage = 60,
                        EchaSubstance = new EchaSubstance
                        {
                            ChemicalName = "Ethanol",
                            CasNumber = "64-17-5",
                            EcNumber = "200-578-6",
                            Hazards = new List<SubstanceHazard>
                            {
                                new SubstanceHazard { PhraseCode = "H225", Phrase = new ClpPhrase { Code = "H225", Description = "Líquido e vapor facilmente inflamáveis" } },
                                new SubstanceHazard { PhraseCode = "H319", Phrase = new ClpPhrase { Code = "H319", Description = "Provoca irritação ocular grave" } }
                            }
                        }
                    },
                    new ProductComposition
                    {
                        Percentage = 40,
                        EchaSubstance = new EchaSubstance
                        {
                            ChemicalName = "Carbon Black",
                            CasNumber = "1333-86-4",
                            EcNumber = "215-609-9",
                            Hazards = new List<SubstanceHazard>
                            {
                                new SubstanceHazard { PhraseCode = "H351", Phrase = new ClpPhrase { Code = "H351", Description = "Suspeito de provocar cancro" } }
                            }
                        }
                    }
                }
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

            if (token.IsCancellationRequested) return;
            
            var tempPdfPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            await File.WriteAllBytesAsync(tempPdfPath, pdfBytes, token);
            
            if (token.IsCancellationRequested) return;
            
            var imagePaths = await _pdfPreviewService.RenderPdfToImagesAsync(tempPdfPath);
            
            if (token.IsCancellationRequested) return;

            // Update UI on main thread automatically because we are in the ViewModel context, but RenderPdfToImagesAsync usually runs on threadpool.
            // If we run into thread affinity issues, we might need a DispatcherQueue, but typically ObservableCollection updates here are fine.
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcher != null)
            {
                dispatcher.TryEnqueue(() =>
                {
                    PdfPreviewImages.Clear();
                    foreach (var imgPath in imagePaths)
                    {
                        var bitmap = new BitmapImage(new Uri(imgPath));
                        PdfPreviewImages.Add(bitmap);
                    }
                    IsPdfPreviewVisible = true;
                    IsLoadingPreview = false;
                });
            }
            else
            {
                PdfPreviewImages.Clear();
                foreach (var imgPath in imagePaths)
                {
                    var bitmap = new BitmapImage(new Uri(imgPath));
                    PdfPreviewImages.Add(bitmap);
                }
                IsPdfPreviewVisible = true;
                IsLoadingPreview = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Omit error handling for cancellation
            IsLoadingPreview = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TemplateEditorViewModel] Preview Error: {ex.Message}");
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
            SyncBuilderToJson();
            
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
    private void GenerateDefaultLayout()
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

        if (Template.DocumentType == "SDS")
        {
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 1: Identification of the substance/mixture and of the company/undertaking", Type = "SdsSection", DataSource = "SdsSection1", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 2: Hazards identification", Type = "SdsSection", DataSource = "SdsSection2", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 3: Composition/information on ingredients", Type = "SdsSection", DataSource = "SdsSection3", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 4: First aid measures", Type = "SdsSection", DataSource = "SdsSection4", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 5: Firefighting measures", Type = "SdsSection", DataSource = "SdsSection5", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 6: Accidental release measures", Type = "SdsSection", DataSource = "SdsSection6", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 7: Handling and storage", Type = "SdsSection", DataSource = "SdsSection7", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 8: Exposure controls/personal protection", Type = "SdsSection", DataSource = "SdsSection8", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 9: Physical and chemical properties", Type = "SdsSection", DataSource = "SdsSection9", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 10: Stability and reactivity", Type = "SdsSection", DataSource = "SdsSection10", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 11: Toxicological information", Type = "SdsSection", DataSource = "SdsSection11", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 12: Ecological information", Type = "SdsSection", DataSource = "SdsSection12", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 13: Disposal considerations", Type = "SdsSection", DataSource = "SdsSection13", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 14: Transport information", Type = "SdsSection", DataSource = "SdsSection14", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 15: Regulatory information", Type = "SdsSection", DataSource = "SdsSection15", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
            BuilderModel.Sections.Add(new TemplateSection { Title = "SECTION 16: Other information", Type = "SdsSection", DataSource = "SdsSection16", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", IsDataBound = true });
        }
        else
        {
            BuilderModel.Sections.Add(new TemplateSection { Title = "Ink features", Type = "BulletPoints", DataSource = "Features", IsHalfWidth = true, FontFamily = "Segoe UI", TextColor = "#223145" });
            BuilderModel.Sections.Add(new TemplateSection { Title = "Ink properties", Type = "Table", DataSource = "LiquidProperties", IsHalfWidth = true, FontFamily = "Segoe UI", TextColor = "#223145" });
            BuilderModel.Sections.Add(new TemplateSection { Title = "Handling Guidelines", Type = "Table", DataSource = "MechanicalProperties", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145" });
            BuilderModel.Sections.Add(new TemplateSection { Title = "Sensor specifications", Type = "Table", DataSource = "Tds", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145", Content = "" });
            BuilderModel.Sections.Add(new TemplateSection { Title = "Performance charts", Type = "Charts", DataSource = "Charts", IsHalfWidth = false, FontFamily = "Segoe UI", TextColor = "#223145" });
        }
        
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

        if (string.IsNullOrWhiteSpace(section.Content) && section.Type != null && section.Type.StartsWith("SdsSection"))
        {
            section.Content = GetDefaultSdsHtml(section.Type);
        }

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
        BuilderModel.Sections.Add(new TemplateSection { IsTdsContext = IsTdsSelected, IsSdsContext = IsSdsSelected });
    }

    [RelayCommand]
    private void RemoveSection(TemplateSection section)
    {
        if (section != null && BuilderModel.Sections.Contains(section))
        {
            BuilderModel.Sections.Remove(section);
        }
    }

    private string GetDefaultSdsHtml(string sectionType)
    {
        return sectionType switch
        {
            "SdsSection1" => @"<p><strong>1.1 Product identifier</strong></p>
<p style=""padding-left:10px"">Product name: {{Name}}</p>
<p><strong>1.2 Relevant identified uses of the substance or mixture and uses advised against</strong></p>
<p style=""padding-left:10px"">Use of the Substance/Mixture: Industrial and research</p>
<p><strong>1.3 Details of the supplier of the safety data sheet</strong></p>
<p style=""padding-left:10px"">Company: {{Company.Name}}<br/>Address: {{Company.Address}}<br/>Telephone: {{Company.Telephone}}<br/>Email: {{Company.Email}}</p>
<p><strong>1.4 Emergency telephone number</strong></p>
<p style=""padding-left:10px"">{{Company.EmergencyTelephone}}</p>",
            "SdsSection2" => @"<p><strong>2.1 Classification of the substance or mixture</strong></p>
<p>Classification (REGULATION (EC) No 1272/2008)</p>
<ul style=""list-style:none; padding-left:10px; margin:0;"">
  {{#if HazardPhrases}}
    {{#each HazardPhrases}}<li>{{this}}</li>{{/each}}
  {{else}}
    <li>Not classified as a hazardous mixture.</li>
  {{/if}}
</ul>
<p style=""margin-top:4px;""><strong>2.2 Label elements</strong></p>
<div style=""padding-left:10px;"">
  {{#each Pictograms}}<div>{{this}}</div>{{/each}}
</div>",
            "SdsSection3" => @"<table class=""sds-table"">
  <tr class=""sds-table-header""><th colspan=""5"">3. COMPOSITION/INFORMATION ON INGREDIENTS</th></tr>
  <tr>
    <th>Chemical Name</th>
    <th>CAS No</th>
    <th>EC No</th>
    <th>Concentration</th>
    <th>Hazards (H Codes)</th>
  </tr>
  {{#each Components}}
  <tr>
    <td>{{ChemicalName}}</td>
    <td>{{CasNumber}}</td>
    <td>{{EcNumber}}</td>
    <td>{{Percentage}}</td>
    <td>{{Hazards}}</td>
  </tr>
  {{/each}}
</table>",
            "SdsSection9" => @"<table class=""sds-table"">
  <tr class=""sds-table-header""><th colspan=""2"">9. PHYSICAL AND CHEMICAL PROPERTIES</th></tr>
  <tr><td width=""40%""><b>Physical State</b></td><td>Liquid</td></tr>
  {{#each LiquidProperties}}
  <tr><td><b>{{@key}}</b></td><td>{{this}}</td></tr>
  {{/each}}
</table>",
            _ => ""
        };
    }
}
