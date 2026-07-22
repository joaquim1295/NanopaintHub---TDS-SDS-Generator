using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class TemplateEditorPage : Page
{
    public TemplateEditorViewModel ViewModel { get; }

    public Microsoft.UI.Xaml.Visibility NullableBoolToVisibility(bool? isChecked) 
        => isChecked == true ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public TemplateEditorPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<TemplateEditorViewModel>();
        DataContext = ViewModel;
        
        ViewModel.PreviewPdfGenerated += ViewModel_PreviewPdfGenerated;
        this.Unloaded += TemplateEditorPage_Unloaded;
    }

    private void TemplateEditorPage_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.PreviewPdfGenerated -= ViewModel_PreviewPdfGenerated;
        PdfWebView?.Close();
    }

    private void RemoveSection_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is NanopaintHub_WinUIClient.Models.TemplateSection section)
        {
            ViewModel.RemoveSectionCommand.Execute(section);
        }
    }

    private void EditSectionContent_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ((sender as Microsoft.UI.Xaml.FrameworkElement)?.DataContext is NanopaintHub_WinUIClient.Models.TemplateSection section)
        {
            ViewModel.EditSectionContentCommand.Execute(section);
        }
    }

    private Windows.Storage.Streams.IRandomAccessStream? _activePdfStream;

    private async void ViewModel_PreviewPdfGenerated(object? sender, Windows.Storage.Streams.IRandomAccessStream stream)
    {
        try
        {
            await EnsureWebViewInitializedAsync();

            _activePdfStream = stream;
            // Use http://localhost to avoid DNS resolution ERR_NAME_NOT_RESOLVED
            PdfWebView.Source = new Uri($"http://localhost/nanopaint-preview/document.pdf?id={Guid.NewGuid()}#page=1&zoom=page-width");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update PDF preview: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task EnsureWebViewInitializedAsync()
    {
        try
        {
            if (PdfWebView.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("[WEBVIEW2-DIAGNOSTIC] A iniciar EnsureCoreWebView2Async...");
                var appData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub", "WebView2");
                System.IO.Directory.CreateDirectory(appData);
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", appData);
                await PdfWebView.EnsureCoreWebView2Async();
                System.Diagnostics.Debug.WriteLine("[WEBVIEW2-DIAGNOSTIC] CoreWebView2 inicializado com sucesso.");
                
                PdfWebView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
                
                // Intercept localhost instead of a fake domain to avoid DNS errors
                PdfWebView.CoreWebView2.AddWebResourceRequestedFilter("http://localhost/nanopaint-preview/*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
                PdfWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

                // Add navigation events for diagnostic verbosity
                PdfWebView.CoreWebView2.NavigationStarting += (s, e) => System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] NavigationStarting: {e.Uri}");
                PdfWebView.CoreWebView2.NavigationCompleted += (s, e) => System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] NavigationCompleted: Success={e.IsSuccess}, ErrorStatus={e.WebErrorStatus}");
                PdfWebView.CoreWebView2.ProcessFailed += (s, e) => System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] ProcessFailed: {e.ProcessFailedKind}, Reason={e.Reason}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] ERRO FATAL na inicialização do WebView2: {ex}");
        }
    }

    private void CoreWebView2_WebResourceRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] WebResourceRequested para URI: {args.Request.Uri}");
        try
        {
            if (args.Request.Uri.StartsWith("http://localhost/nanopaint-preview/document.pdf") && _activePdfStream != null)
            {
                _activePdfStream?.Seek(0);
                args.Response = sender.Environment.CreateWebResourceResponse(
                    _activePdfStream, 200, "OK", "Content-Type: application/pdf");
                System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] Stream acoplado à Response (Tamanho: {_activePdfStream?.Size} bytes)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] Pedido ignorado (Stream nulo ou URI inválido). Stream is null: {_activePdfStream == null}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WEBVIEW2-DIAGNOSTIC] Erro ao servir WebResource: {ex}");
        }
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

    private void NavigationToggle_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel == null) return;
        
        if (sender is Microsoft.UI.Xaml.Controls.Primitives.ToggleButton tb)
        {
            // Enforce mutual exclusivity
            if (tb != TagTheme && TagTheme != null) TagTheme.IsChecked = false;
            if (tb != TagHeader && TagHeader != null) TagHeader.IsChecked = false;
            if (tb != TagFooter && TagFooter != null) TagFooter.IsChecked = false;
            if (tb != TagBatch && TagBatch != null) TagBatch.IsChecked = false;
            if (tb != TagSections && TagSections != null) TagSections.IsChecked = false;
            if (tb != TagCode && TagCode != null) TagCode.IsChecked = false;

            // Handle panel visibility
            if (PanelTheme != null) PanelTheme.Visibility = (tb == TagTheme) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (PanelHeader != null) PanelHeader.Visibility = (tb == TagHeader) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (PanelFooter != null) PanelFooter.Visibility = (tb == TagFooter) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (PanelBatch != null) PanelBatch.Visibility = (tb == TagBatch) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (PanelSections != null) PanelSections.Visibility = (tb == TagSections) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (PanelCode != null) PanelCode.Visibility = (tb == TagCode) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

            if (tb.Name == "TagCode")
            {
                ViewModel.SyncBuilderToHtml();
            }
            else
            {
                ViewModel.SyncHtmlToBuilder();
            }
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

    private void TogglePreview_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (TogglePreviewBtn == null || PreviewColumn == null || PreviewSplitter == null || PreviewContainer == null) return;
        
        if (TogglePreviewBtn.IsChecked == true)
        {
            PreviewColumn.Width = new Microsoft.UI.Xaml.GridLength(4, Microsoft.UI.Xaml.GridUnitType.Star);
            PreviewSplitter.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            PreviewContainer.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        else
        {
            PreviewColumn.Width = new Microsoft.UI.Xaml.GridLength(0);
            PreviewSplitter.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            PreviewContainer.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private void DocumentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Forçar binding source update
        if (sender is ComboBox cb && cb.SelectedValue != null && ViewModel?.Template != null)
        {
            ViewModel.Template.DocumentType = cb.SelectedValue.ToString() ?? string.Empty;
        }
        
        ViewModel?.UpdateDocumentTypeFlags();
        
        // When Document Type changes, we stay on the current tab, no need to force visibility changes here
        // as the Sections tab is now valid for both SDS and TDS.
    }
}
