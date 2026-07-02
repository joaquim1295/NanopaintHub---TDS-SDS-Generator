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

    private void SubstanceSearchResults_ItemClick(object sender, Microsoft.UI.Xaml.Controls.ItemClickEventArgs e)
    {
        if (e.ClickedItem is NanopaintHub.Domain.Entities.EchaSubstance substance)
        {
            ViewModel.AddCompositionCommand.Execute(substance);
            
            SubstanceSearchBox.Text = string.Empty;
            SubstanceSearchBoxWide.Text = string.Empty;
            
            SubstanceSearchResultsPopup.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            SubstanceSearchResultsPopupWide.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private void CompositionPercentage_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
    {
        // Enqueue the validation so it runs after the binding updates the model
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.ValidateCompositions();
        });
    }
}
