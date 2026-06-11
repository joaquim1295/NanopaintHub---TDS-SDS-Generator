using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class ProductsPage : Page
{
    public ProductsViewModel ViewModel { get; }

    public ProductsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ProductsViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.LoadProductsCommand.ExecuteAsync(null);
    }
}
