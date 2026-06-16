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
                
                var sourceStorageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(file.Path);
                var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(dataDir);
                var copiedFile = await sourceStorageFile.CopyAsync(folder, file.Name, Windows.Storage.NameCollisionOption.GenerateUniqueName);

                ViewModel.BuilderModel.Header.LogoUrl = "file:///" + copiedFile.Path.Replace("\\", "/");
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

    

    private async void GlobalTinyMce_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Se ainda não estava personalizado, geramos a versão mais atual do visual builder
        if (!ViewModel.IsCustomizedHtml)
        {
            ViewModel.SyncBuilderToHtml();
        }

        var currentHtml = ViewModel.Template?.HtmlStructure ?? "";

        var quillService = App.Services.GetRequiredService<NanopaintHub.Application.Contracts.IChromiumTinyMceEditorService>();
        var assetsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
        var cssTheme = NanopaintHub_WinUIClient.Services.TdsHtmlGenerator.GenerateCss(ViewModel.BuilderModel);
        
        var result = await quillService.OpenEditorAsync(
            currentHtml, 
            assetsPath, 
            cssTheme,
            NanopaintHub.Application.Contracts.ChromiumTemplateEditorMode.WordLike);

        if (result.Applied && !string.IsNullOrWhiteSpace(result.Html))
        {
            if (ViewModel.Template != null)
            {
                ViewModel.Template.HtmlStructure = result.Html;
                ViewModel.IsCustomizedHtml = true;
                ViewModel.SyncHtmlToBuilder();
            }
        }
    }

    private void HeaderRepeatStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is string style)
        {
            ViewModel.BuilderModel.Header.HeaderRepeatStyle = style;
        }
    }

    private void FooterRepeatStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is string style)
        {
            ViewModel.BuilderModel.Footer.FooterRepeatStyle = style;
        }
    }
}
