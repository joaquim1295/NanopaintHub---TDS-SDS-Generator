using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using NanopaintHub.Infrastructure.Documents.DependencyInjection;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly string _appSettingsPath;

    public List<string> AvailableEngines { get; } = new() { "QuestPDF", "WebView2", "iText7" };

    [ObservableProperty]
    private string _tdsEngine;

    [ObservableProperty]
    private string _sdsEngine;

    public SettingsViewModel(IOptionsMonitor<PdfGeneratorOptions> options)
    {
        _tdsEngine = options.CurrentValue.TdsEngine;
        _sdsEngine = options.CurrentValue.SdsEngine;
        
        // Use local app data for user settings
        var localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub");
        _appSettingsPath = Path.Combine(localFolder, "appsettings.user.json");
    }

    partial void OnTdsEngineChanged(string value)
    {
        SaveSettingsAsync();
    }

    partial void OnSdsEngineChanged(string value)
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

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_appSettingsPath, root.ToJsonString(options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
