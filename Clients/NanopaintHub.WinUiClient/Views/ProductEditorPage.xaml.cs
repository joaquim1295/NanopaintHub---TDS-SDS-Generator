using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class ProductEditorPage : Page
{
    public ProductEditorViewModel ViewModel { get; }

    public ProductEditorPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ProductEditorViewModel>();
        DataContext = ViewModel;
    }

    private void NavigationToggle_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Primitives.ToggleButton selectedBtn)
        {
            if (selectedBtn.IsChecked == false)
            {
                selectedBtn.IsChecked = true;
                return;
            }

            var parent = selectedBtn.Parent as StackPanel;
            if (parent != null)
            {
                foreach (var child in parent.Children)
                {
                    if (child is Microsoft.UI.Xaml.Controls.Primitives.ToggleButton btn && btn != selectedBtn)
                    {
                        btn.IsChecked = false;
                    }
                }
            }

            if (GeneralSection != null) GeneralSection.Visibility = selectedBtn.Name == "TagGeneral" ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (PropertiesSection != null) PropertiesSection.Visibility = selectedBtn.Name == "TagProperties" ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (SafetySection != null) SafetySection.Visibility = selectedBtn.Name == "TagSafety" ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            if (SensorSection != null) SensorSection.Visibility = selectedBtn.Name == "TagSensor" ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private void TogglePreview_Changed(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (TogglePreviewBtn == null || PreviewContainer == null || PreviewSplitter == null) return;
        
        var isChecked = TogglePreviewBtn.IsChecked == true;
        PreviewContainer.Visibility = isChecked ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        PreviewSplitter.Visibility = isChecked ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        var grid = PreviewContainer.Parent as Grid;
        if (grid != null)
        {
            if (isChecked)
            {
                grid.ColumnDefinitions[3].Width = new Microsoft.UI.Xaml.GridLength(3, Microsoft.UI.Xaml.GridUnitType.Star);
                grid.ColumnDefinitions[2].Width = Microsoft.UI.Xaml.GridLength.Auto;
            }
            else
            {
                grid.ColumnDefinitions[3].Width = new Microsoft.UI.Xaml.GridLength(0);
                grid.ColumnDefinitions[2].Width = new Microsoft.UI.Xaml.GridLength(0);
            }
        }
    }

    private void AutoTriggerPreview_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        ViewModel?.TriggerPreviewUpdate();
    }

    private void AutoTriggerPreview_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        ViewModel?.TriggerPreviewUpdate();
    }

    private void AutoTriggerPreview_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs e)
    {
        ViewModel?.TriggerPreviewUpdate();
    }

    private void AutoTriggerPreview_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel?.TriggerPreviewUpdate();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Guid? productId = null;
        if (e.Parameter is Guid id)
        {
            productId = id;
        }
        
        _ = ViewModel.InitializeAsync(productId);
    }

    private void SubstanceSearchBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        var textBox = sender as Microsoft.UI.Xaml.Controls.TextBox;
        if (textBox == null) return;
        
        var query = textBox.Text;
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            SubstanceSearchResultsPopup.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            SubstanceSearchResultsPopupWide.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        ViewModel.SearchSubstancesCommand.ExecuteAsync(query).ContinueWith(_ =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var hasResults = ViewModel.SubstanceSearchResults != null && System.Linq.Enumerable.Any(ViewModel.SubstanceSearchResults);
                SubstanceSearchResultsPopup.Visibility = hasResults ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
                SubstanceSearchResultsPopupWide.Visibility = hasResults ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
            });
        });
    }

    private async void SubstanceSearchResults_ItemClick(object sender, Microsoft.UI.Xaml.Controls.ItemClickEventArgs e)
    {
        if (e.ClickedItem is NanopaintHub.Application.DTOs.FederatedSearchResult result)
        {
            if (result.IsImportAction)
            {
                SubstanceSearchResultsPopup.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                SubstanceSearchResultsPopupWide.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                await ImportPdfAsync(result.TypedQuery);
                return;
            }

            ViewModel.AddCompositionCommand.Execute(result);
            
            SubstanceSearchBox.Text = string.Empty;
            SubstanceSearchBoxWide.Text = string.Empty;
            
            SubstanceSearchResultsPopup.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            SubstanceSearchResultsPopupWide.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }


    private async System.Threading.Tasks.Task ImportPdfAsync(string initialQuery = "")
    {
        var window = ((App)App.Current).MainWindow;
        if (window == null) return;
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        try
        {
            var pdfExtractor = App.Services.GetRequiredService<NanopaintHub.Application.Contracts.ISdsPdfExtractorService>();
            var extractedData = await pdfExtractor.ExtractSdsDataAsync(file.Path);

            if (extractedData.CasNumbers.Count == 0 && string.IsNullOrWhiteSpace(extractedData.ProductName))
            {
                var dialog = new ContentDialog
                {
                    Title = "Nenhum dado encontrado",
                    Content = "Não foi possível extrair dados estruturados ou números CAS deste PDF.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            var echaRepo = App.Services.GetRequiredService<NanopaintHub.Application.Contracts.IEchaSubstanceRepository>();
            var supplierRepo = App.Services.GetRequiredService<NanopaintHub.Application.Contracts.ISupplierRepository>();
            var supplierProductRepo = App.Services.GetRequiredService<NanopaintHub.Application.Contracts.ISupplierProductRepository>();

            var substances = new System.Collections.Generic.List<NanopaintHub.Domain.Entities.EchaSubstance>();
            foreach (var data in extractedData.CasNumbers)
            {
                data.CasNumber = data.CasNumber?.Trim() ?? string.Empty;
                var sub = await echaRepo.GetByCasAsync(data.CasNumber);
                if (sub == null && !string.IsNullOrWhiteSpace(data.CasNumber))
                {
                    var confirmMissingDialog = new ContentDialog
                    {
                        Title = "CAS não encontrado na ECHA",
                        Content = $"O CAS {data.CasNumber} não foi encontrado na base de dados local do Anexo Harmonizado II da ECHA.\n\nSerá importado utilizando os dados extraídos diretamente da Ficha de Segurança (SDS). Deseja continuar?",
                        PrimaryButtonText = "Continuar",
                        CloseButtonText = "Ignorar Componente",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot
                    };
                    var missingResult = await confirmMissingDialog.ShowAsync();
                    if (missingResult != ContentDialogResult.Primary)
                    {
                        continue;
                    }

                    sub = new NanopaintHub.Domain.Entities.EchaSubstance
                    {
                        CasNumber = data.CasNumber,
                        ChemicalName = "Substância Importada (CAS: " + data.CasNumber + ")"
                    };
                    
                    if (data.ExtractedHazards != null && data.ExtractedHazards.Any())
                    {
                        foreach (var hazard in data.ExtractedHazards)
                        {
                            sub.Hazards.Add(new NanopaintHub.Domain.Entities.SubstanceHazard
                            {
                                Substance = sub,
                                PhraseCode = hazard
                            });
                        }
                    }

                    await echaRepo.AddAsync(sub);
                }
                
                if (sub != null) 
                {
                    // Enrich existing database component with any newly extracted hazards from the SDS
                    if (data.ExtractedHazards != null && data.ExtractedHazards.Any())
                    {
                        foreach (var hazard in data.ExtractedHazards)
                        {
                            if (!sub.Hazards.Any(h => h.PhraseCode == hazard))
                            {
                                sub.Hazards.Add(new NanopaintHub.Domain.Entities.SubstanceHazard
                                {
                                    Substance = sub,
                                    PhraseCode = hazard
                                });
                            }
                        }
                    }

                    substances.Add(sub);
                }
            }

            if (string.IsNullOrWhiteSpace(extractedData.ProductName))
            {
                extractedData.ProductName = initialQuery;
            }

            var nameTextBox = new TextBox { Header = "Nome do Produto", Text = extractedData.ProductName, PlaceholderText = "Ex: Resina X", Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 16) };
            var supplierTextBox = new TextBox { Header = "Fornecedor", Text = extractedData.SupplierName, PlaceholderText = "Ex: BASF", Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 16) };
            
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = $"Foram encontrados {extractedData.CasNumbers.Count} componentes ({substances.Count} identificados na base ECHA).", Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 16), TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap });
            
            // Anti-duplicate protection: Warning bar
            var duplicateWarning = new Microsoft.UI.Xaml.Controls.InfoBar
            {
                Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                Title = "Possível Duplicado",
                Message = "Já existe um produto com um nome semelhante na base de dados.",
                IsOpen = false,
                IsClosable = false,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 16)
            };
            panel.Children.Add(duplicateWarning);
            
            panel.Children.Add(nameTextBox);
            panel.Children.Add(supplierTextBox);

            // Dynamically check for duplicates when name changes
            nameTextBox.TextChanged += async (s, args) => 
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text) || nameTextBox.Text.Length < 3)
                {
                    duplicateWarning.IsOpen = false;
                    return;
                }
                var similar = await supplierProductRepo.SearchByNameAsync(nameTextBox.Text);
                duplicateWarning.IsOpen = similar != null && similar.Count > 0;
            };

            // Trigger initial check
            if (!string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                var initialSimilar = await supplierProductRepo.SearchByNameAsync(nameTextBox.Text);
                duplicateWarning.IsOpen = initialSimilar != null && initialSimilar.Count > 0;
            }

            // UI for Mixture of Mixtures (Substances extracted)
            var compositionsPanel = new StackPanel { Spacing = 8, Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0) };
            compositionsPanel.Children.Add(new TextBlock { Text = "Composição Interna Extraída:", Style = (Microsoft.UI.Xaml.Style)Microsoft.UI.Xaml.Application.Current.Resources["BodyStrongTextBlockStyle"] });
            
            foreach (var sub in substances)
            {
                var casData = extractedData.CasNumbers.FirstOrDefault(c => c.CasNumber == sub.CasNumber);
                double autoPct = casData?.ParsedPercentage ?? 0;
                string rawPctInfo = string.IsNullOrWhiteSpace(casData?.RawPercentageText) ? "" : $" [{casData.RawPercentageText}]";
                
                string hazardsStr = string.Join(", ", sub.Hazards?.Select(h => h.PhraseCode) ?? Array.Empty<string>());
                if (string.IsNullOrWhiteSpace(hazardsStr)) hazardsStr = "Sem classificação";
                
                var subText = new TextBlock { Text = $"• {sub.ChemicalName} (CAS: {sub.CasNumber}) - {autoPct}%{rawPctInfo}\n   Classificação: {hazardsStr}", VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap };
                compositionsPanel.Children.Add(subText);
            }

            if (substances.Count > 0)
            {
                panel.Children.Add(compositionsPanel);
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Importar Matéria-Prima de SDS",
                Content = panel,
                PrimaryButtonText = "Salvar e Adicionar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                // Create supplier if not exists (simplification)
                var supplierName = string.IsNullOrWhiteSpace(supplierTextBox.Text) ? "Desconhecido" : supplierTextBox.Text;
                var suppliers = await supplierRepo.GetAllAsync();
                var supplier = System.Linq.Enumerable.FirstOrDefault(suppliers, s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));
                
                if (supplier == null)
                {
                    supplier = new NanopaintHub.Domain.Entities.Supplier { Name = supplierName };
                    await supplierRepo.AddAsync(supplier);
                }

                // Copy PDF to secure archive
                string archiveDir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "NanopaintHub", "SdsArchives");
                System.IO.Directory.CreateDirectory(archiveDir);
                string extension = System.IO.Path.GetExtension(file.Path);
                string safeFileName = $"{Guid.NewGuid()}{extension}";
                string destPath = System.IO.Path.Combine(archiveDir, safeFileName);
                System.IO.File.Copy(file.Path, destPath, true);

                var supplierProduct = new NanopaintHub.Domain.Entities.SupplierProduct
                {
                    Name = nameTextBox.Text,
                    SupplierId = supplier.Id,
                    Supplier = supplier,
                    SdsFilePath = destPath
                };

                foreach (var sub in substances)
                {
                    var casData = extractedData.CasNumbers.FirstOrDefault(c => c.CasNumber == sub.CasNumber);
                    double autoPct = casData?.ParsedPercentage ?? 0;

                    supplierProduct.Compositions.Add(new NanopaintHub.Domain.Entities.SupplierProductComposition
                    {
                        EchaSubstanceId = sub.Id,
                        EchaSubstance = sub,
                        SupplierProductId = supplierProduct.Id,
                        Percentage = autoPct,
                        PercentageText = casData?.RawPercentageText
                    });
                }

                await supplierProductRepo.AddAsync(supplierProduct);

                // Add the SupplierProduct itself to the Paint
                ViewModel.AddCompositionCommand.Execute(new NanopaintHub.Application.DTOs.FederatedSearchResult
                {
                    SupplierProduct = supplierProduct
                });
            }
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Erro ao extrair PDF",
                Content = ex.Message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void OpenDescriptionDialog_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (DescriptionDialog != null)
        {
            DescriptionDialog.XamlRoot = this.XamlRoot;
            DescriptionTextBox.Text = ViewModel.Product?.Description ?? string.Empty;

            var result = await DescriptionDialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                ViewModel.Product.Description = DescriptionTextBox.Text;
                ViewModel.TriggerPreviewUpdate();
            }
        }
    }

    #region Performance Charts File Pickers

    private async System.Threading.Tasks.Task<string?> PickCsvOrDataFileAsync()
    {
        var window = ((App)App.Current).MainWindow;
        if (window == null) return null;
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".tsv");
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".xls");
        picker.FileTypeFilter.Add(".xlsx");

        var file = await picker.PickSingleFileAsync();
        if (file == null) return null;
        return await System.IO.File.ReadAllTextAsync(file.Path);
    }

    private async System.Threading.Tasks.Task<string?> PickImageFileAsync()
    {
        var window = ((App)App.Current).MainWindow;
        if (window == null) return null;
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".webp");
        picker.FileTypeFilter.Add(".gif");

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async void ImportLinearityCsv_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var content = await PickCsvOrDataFileAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
            ViewModel.ImportLinearityFromContent(content);
        }
    }

    private async void ImportLinearityImage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.ImportLinearityImagePath(path);
        }
    }

    private void ClearLinearity_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.ClearLinearityChart();
    }

    private async void ImportResistanceCsv_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var content = await PickCsvOrDataFileAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
            ViewModel.ImportResistanceFromContent(content);
        }
    }

    private async void ImportResistanceImage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.ImportResistanceImagePath(path);
        }
    }

    private void ClearResistance_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.ClearResistanceChart();
    }

    private async void ImportHysteresisCsv_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var content = await PickCsvOrDataFileAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
            ViewModel.ImportHysteresisFromContent(content);
        }
    }

    private async void ImportHysteresisImage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.ImportHysteresisImagePath(path);
        }
    }

    private void ClearHysteresis_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.ClearHysteresisChart();
    }

    #endregion
}
