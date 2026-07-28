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

    private double _savedPreviewScrollOffset = 0;

    public TemplateEditorPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<TemplateEditorViewModel>();
        DataContext = ViewModel;
        
        this.Loaded += TemplateEditorPage_Loaded;
        this.Unloaded += TemplateEditorPage_Unloaded;
    }

    private void TemplateEditorPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void TemplateEditorPage_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsLoadingPreview))
        {
            if (ViewModel.IsLoadingPreview)
            {
                _savedPreviewScrollOffset = PreviewScrollViewer?.VerticalOffset ?? 0;
            }
            else
            {
                if (PreviewScrollViewer != null)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                        PreviewScrollViewer.ChangeView(null, _savedPreviewScrollOffset, null, true);
                    });
                }
            }
        }
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

    private async void OpenAddressDialog_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (AddressDialog != null)
        {
            AddressDialog.XamlRoot = this.XamlRoot;
            AddressTextBox.Text = ViewModel.BuilderModel.Footer.Column1 ?? string.Empty;
            
            var result = await AddressDialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                ViewModel.BuilderModel.Footer.Column1 = AddressTextBox.Text;
                _ = ViewModel.GeneratePreviewPdfAsync();
            }
        }
    }

    private async void OpenContactsDialog_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ContactsDialog != null)
        {
            ContactsDialog.XamlRoot = this.XamlRoot;
            ContactsTextBox.Text = ViewModel.BuilderModel.Footer.Column2 ?? string.Empty;
            
            var result = await ContactsDialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                ViewModel.BuilderModel.Footer.Column2 = ContactsTextBox.Text;
                _ = ViewModel.GeneratePreviewPdfAsync();
            }
        }
    }

    private async void OpenLegalTextDialog_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (LegalTextDialog != null)
        {
            LegalTextDialog.XamlRoot = this.XamlRoot;
            LegalTextBox.Text = ViewModel.BuilderModel.Footer.LegalText ?? string.Empty;
            
            var result = await LegalTextDialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                ViewModel.BuilderModel.Footer.LegalText = LegalTextBox.Text;
                _ = ViewModel.GeneratePreviewPdfAsync();
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

    private int _questPdfInfoBarTimeoutId = 0;
    
    private async void DocumentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Forçar binding source update
        if (sender is ComboBox cb && cb.SelectedValue != null && ViewModel?.Template != null)
        {
            ViewModel.Template.DocumentType = cb.SelectedValue.ToString() ?? string.Empty;
        }
        
        ViewModel?.UpdateDocumentTypeFlags();
        
        if (ViewModel != null && ViewModel.IsQuestPdfSelected && QuestPdfInfoBar != null)
        {
            QuestPdfInfoBar.IsOpen = true;
            var currentId = ++_questPdfInfoBarTimeoutId;
            
            await System.Threading.Tasks.Task.Delay(5000);
            
            // Apenas fecha se não tiver havido uma nova seleção pelo meio
            if (currentId == _questPdfInfoBarTimeoutId && QuestPdfInfoBar != null)
            {
                QuestPdfInfoBar.IsOpen = false;
            }
        }
        
        // Auto-preview
        _ = ViewModel?.GeneratePreviewPdfAsync();
        
        // When Document Type changes, we stay on the current tab, no need to force visibility changes here
        // as the Sections tab is now valid for both SDS and TDS.
    }

    private void UpdatePreview_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _ = ViewModel.GeneratePreviewPdfAsync();
    }

    private void AutoTriggerPreview_Changed(object sender, object e)
    {
        _ = ViewModel.GeneratePreviewPdfAsync();
    }

    private void HeaderFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CommunityToolkit.WinUI.Controls.Segmented segmented)
        {
            if (ContainerTextos == null || ContainerLogo == null || ContainerLayout == null) return;
            
            int index = segmented.SelectedIndex;
            
            ContainerTextos.Visibility = (index == 0 || index == 1) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            ContainerLogo.Visibility = (index == 0 || index == 2) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            ContainerLayout.Visibility = (index == 0 || index == 3) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }
}
