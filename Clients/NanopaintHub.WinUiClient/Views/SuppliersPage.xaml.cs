namespace NanopaintHub_WinUIClient.Views;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub.WinUiClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

public sealed partial class SuppliersPage : Page
{
    public SuppliersViewModel ViewModel { get; }

    public SuppliersPage()
    {
        ViewModel = App.Services.GetRequiredService<SuppliersViewModel>();
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDataAsync();
    }
}
