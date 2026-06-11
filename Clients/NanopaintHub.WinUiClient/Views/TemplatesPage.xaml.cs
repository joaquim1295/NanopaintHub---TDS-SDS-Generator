using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class TemplatesPage : Page
{
    public TemplatesViewModel ViewModel { get; }

    public TemplatesPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<TemplatesViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.LoadTemplatesCommand.ExecuteAsync(null);
    }
}
