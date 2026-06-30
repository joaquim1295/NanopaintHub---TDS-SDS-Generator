using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json.Nodes;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isBackEnabled;

    [ObservableProperty]
    private NavigationViewItem? _selectedItem;

    [ObservableProperty]
    private bool _showEchaAlert;

    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.Navigated += OnNavigated;
        CheckEchaDatabaseExpiration();
    }

    private void CheckEchaDatabaseExpiration()
    {
        try
        {
            var localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub");
            var appSettingsPath = Path.Combine(localFolder, "appsettings.user.json");
            
            int alertThresholdMonths = 6;
            int alertDurationSeconds = 15;

            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                var root = JsonNode.Parse(json)?.AsObject();
                
                if (root != null && root.ContainsKey("EchaDatabase") && root["EchaDatabase"] is JsonObject echaNode)
                {
                    if (echaNode.ContainsKey("AlertThresholdMonths") && int.TryParse(echaNode["AlertThresholdMonths"]?.ToString(), out int threshold))
                    {
                        alertThresholdMonths = threshold;
                    }
                    if (echaNode.ContainsKey("AlertDurationSeconds") && int.TryParse(echaNode["AlertDurationSeconds"]?.ToString(), out int duration))
                    {
                        alertDurationSeconds = duration;
                    }

                    if (echaNode.ContainsKey("LastImportDate") && DateTime.TryParse(echaNode["LastImportDate"]?.ToString(), out DateTime lastImport))
                    {
                        var monthsSinceImport = (DateTime.UtcNow - lastImport).TotalDays / 30.0;
                        if (monthsSinceImport >= alertThresholdMonths)
                        {
                            ShowTemporaryAlert(alertDurationSeconds);
                        }
                        return;
                    }
                }
            }

            // Se o ficheiro não existe, ou não tem data de importação, mostramos o alerta como default
            ShowTemporaryAlert(alertDurationSeconds);
        }
        catch
        {
            // Fail silently
        }
    }

    private async void ShowTemporaryAlert(int durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            ShowEchaAlert = true; // Se configurarem para 0, não esconde automaticamente
            return;
        }

        ShowEchaAlert = true;
        await System.Threading.Tasks.Task.Delay(durationSeconds * 1000);
        ShowEchaAlert = false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            IsBackEnabled = frame.CanGoBack;
        }
    }
}
