using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Infrastructure.Documents.DependencyInjection;
using NanopaintHub.Infrastructure.LocalData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly string _appSettingsPath;
    private readonly IEchaImportService _echaImportService;
    private readonly NanopaintHubDbContext _dbContext;

    public List<string> AvailableEngines { get; } = new() { "QuestPDF", "WebView2", "iText7" };

    [ObservableProperty]
    private string _tdsEngine;

    [ObservableProperty]
    private string _sdsEngine;

    [ObservableProperty]
    private string _importStatusMessage = "";

    [ObservableProperty]
    private int _alertThresholdMonths;

    [ObservableProperty]
    private int _alertDurationSeconds;

    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _totalSuppliers;

    [ObservableProperty]
    private int _totalCompositions;

    [ObservableProperty]
    private int _totalEchaSubstances;

    [ObservableProperty]
    private string _exportedFilePath = string.Empty;

    public bool HasExportedFile => !string.IsNullOrEmpty(ExportedFilePath);

    public SettingsViewModel(IOptionsMonitor<PdfGeneratorOptions> options, IEchaImportService echaImportService, NanopaintHubDbContext dbContext)
    {
        _echaImportService = echaImportService;
        _dbContext = dbContext;
        _tdsEngine = options.CurrentValue.TdsEngine;
        _sdsEngine = options.CurrentValue.SdsEngine;
        
        // Use local app data for user settings
        var localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub");
        _appSettingsPath = Path.Combine(localFolder, "appsettings.user.json");

        LoadSettings();
    }

    private void LoadSettings()
    {
        AlertThresholdMonths = 6;
        AlertDurationSeconds = 15;

        if (File.Exists(_appSettingsPath))
        {
            try
            {
                var json = File.ReadAllText(_appSettingsPath);
                var root = JsonNode.Parse(json)?.AsObject();
                if (root != null && root.ContainsKey("EchaDatabase") && root["EchaDatabase"] is JsonObject echaNode)
                {
                    if (echaNode.ContainsKey("AlertThresholdMonths") && int.TryParse(echaNode["AlertThresholdMonths"]?.ToString(), out int threshold))
                    {
                        AlertThresholdMonths = threshold;
                    }
                    if (echaNode.ContainsKey("AlertDurationSeconds") && int.TryParse(echaNode["AlertDurationSeconds"]?.ToString(), out int duration))
                    {
                        AlertDurationSeconds = duration;
                    }
                }
            }
            catch { }
        }
    }

    partial void OnTdsEngineChanged(string value)
    {
        SaveSettingsAsync();
    }

    partial void OnSdsEngineChanged(string value)
    {
        SaveSettingsAsync();
    }

    partial void OnAlertThresholdMonthsChanged(int value)
    {
        SaveSettingsAsync();
    }

    partial void OnAlertDurationSecondsChanged(int value)
    {
        SaveSettingsAsync();
    }

    private async void SaveSettingsAsync()
    {
        try
        {
            JsonObject root;
            
            if (File.Exists(_appSettingsPath))
            {
                var json = await File.ReadAllTextAsync(_appSettingsPath);
                root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            if (!root.ContainsKey("PdfGenerator"))
            {
                root["PdfGenerator"] = new JsonObject();
            }

            var pdfGen = root["PdfGenerator"]!.AsObject();
            pdfGen["TdsEngine"] = TdsEngine;
            pdfGen["SdsEngine"] = SdsEngine;

            if (!root.ContainsKey("EchaDatabase"))
            {
                root["EchaDatabase"] = new JsonObject();
            }
            var echaNode = root["EchaDatabase"]!.AsObject();
            echaNode["AlertThresholdMonths"] = AlertThresholdMonths;
            echaNode["AlertDurationSeconds"] = AlertDurationSeconds;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_appSettingsPath, root.ToJsonString(options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportEchaCsvAsync()
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        
        // This is needed for WinUI 3 desktop apps
        var app = (Microsoft.UI.Xaml.Application.Current as App);
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".xls");
        picker.FileTypeFilter.Add(".xlsx");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ImportStatusMessage = "A importar dados... Por favor, aguarde.";
            try
            {
                var count = await _echaImportService.ImportCsvAsync(file.Path);
                ImportStatusMessage = $"Importação concluída com sucesso! {count} substâncias adicionadas.";
                SaveLastImportDate();
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"Erro na importação: {ex.Message}";
            }
        }
    }

    private void SaveLastImportDate()
    {
        try
        {
            JsonObject root;
            if (File.Exists(_appSettingsPath))
            {
                var json = File.ReadAllText(_appSettingsPath);
                root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            if (!root.ContainsKey("EchaDatabase"))
            {
                root["EchaDatabase"] = new JsonObject();
            }

            root["EchaDatabase"]!["LastImportDate"] = DateTime.UtcNow.ToString("O");
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_appSettingsPath, root.ToJsonString(options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving EchaDatabase settings: {ex.Message}");
        }
    }

    public async Task LoadDatabaseMetricsAsync()
    {
        try
        {
            TotalProducts = await _dbContext.Products.CountAsync();
            TotalSuppliers = await _dbContext.Suppliers.CountAsync();
            TotalCompositions = await _dbContext.ProductCompositions.CountAsync() + await _dbContext.SupplierProductCompositions.CountAsync();
            TotalEchaSubstances = await _dbContext.EchaSubstances.CountAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading metrics: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportDatabaseAsync()
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        
        var app = (Microsoft.UI.Xaml.Application.Current as App);
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app?.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
        picker.FileTypeChoices.Add("SQLite Database", new List<string>() { ".db" });
        picker.SuggestedFileName = "nanopainthub_inspect.db";

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub");
                var dbPath = Path.Combine(appDataFolder, "nanopainthub.db");

                using (var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var destStream = new FileStream(file.Path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    sourceStream.CopyTo(destStream);
                }
                
                ExportedFilePath = file.Path;
                OnPropertyChanged(nameof(HasExportedFile));

                // Open the folder and select the file instead of trying to open the .db directly
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file.Path}\"");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting database: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void OpenExportedDatabase()
    {
        if (File.Exists(ExportedFilePath))
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{ExportedFilePath}\"");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening database: {ex.Message}");
            }
        }
    }
}
