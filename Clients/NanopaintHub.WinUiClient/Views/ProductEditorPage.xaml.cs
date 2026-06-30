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

    private void SubstanceSearchBox_TextChanged(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == Microsoft.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _ = ViewModel.SearchSubstancesCommand.ExecuteAsync(sender.Text);
        }
    }

    private void SubstanceSearchBox_SuggestionChosen(Microsoft.UI.Xaml.Controls.AutoSuggestBox sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is NanopaintHub.Domain.Entities.EchaSubstance substance)
        {
            ViewModel.AddCompositionCommand.Execute(substance);
            sender.Text = string.Empty;
        }
    }
}
