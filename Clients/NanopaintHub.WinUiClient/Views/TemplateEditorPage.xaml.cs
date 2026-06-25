using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class TemplateEditorPage : Page
{
    public TemplateEditorViewModel ViewModel { get; }

    public TemplateEditorPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<TemplateEditorViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Guid? templateId = null;
        if (e.Parameter is Guid id)
        {
            templateId = id;
        }
        
        _ = ViewModel.InitializeAsync(templateId);
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var pivot = sender as Pivot;
        if (pivot?.SelectedIndex == 1) // Advanced Tab
        {
            ViewModel.SyncBuilderToHtml();
        }
        else if (pivot?.SelectedIndex == 0) // Builder Tab
        {
            ViewModel.SyncHtmlToBuilder();
        }
        else if (pivot?.SelectedIndex == 2) // Preview Tab
        {
            _ = ViewModel.GeneratePreviewPdfAsync();
        }
    }

    private async void PickLogo_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var mainWindow = (App.Current as App)?.MainWindow;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        
        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".svg");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            try
            {
                var dataDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NanopaintHub", "Assets", "Logos");
                System.IO.Directory.CreateDirectory(dataDir);
                
                var destPath = System.IO.Path.Combine(dataDir, file.Name);
                
                var normalizedFilePath = System.IO.Path.GetFullPath(file.Path);
                var normalizedDestPath = System.IO.Path.GetFullPath(destPath);
                
                if (string.Equals(normalizedFilePath, normalizedDestPath, StringComparison.OrdinalIgnoreCase))
                {
                    ViewModel.BuilderModel.Header.LogoUrl = "file:///" + file.Path.Replace("\\", "/");
                }
                else
                {
                    var sourceStorageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(file.Path);
                    var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(dataDir);
                    var copiedFile = await sourceStorageFile.CopyAsync(folder, file.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName);

                    ViewModel.BuilderModel.Header.LogoUrl = "file:///" + copiedFile.Path.Replace("\\", "/");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao copiar logo: {ex}");
                ViewModel.BuilderModel.Header.LogoUrl = "file:///" + file.Path.Replace("\\", "/");
            }
        }
    }

    private void CloseColorFlyout_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement fe && fe.XamlRoot != null)
        {
            var popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(fe.XamlRoot);
            foreach (var popup in popups)
            {
                popup.IsOpen = false;
            }
        }
    }

}
