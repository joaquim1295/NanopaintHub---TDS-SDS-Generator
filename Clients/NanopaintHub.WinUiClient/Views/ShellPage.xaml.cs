using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.Services;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }
    private readonly INavigationService _navigationService;

    public ShellPage()
    {
        InitializeComponent();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        ViewModel = App.Services.GetRequiredService<ShellViewModel>();
        DataContext = ViewModel;

        Loaded += ShellPage_Loaded;
    }

    private void ShellPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _navigationService.Initialize(ContentFrame);
        // Default navigate to Dashboard
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        _navigationService.NavigateTo("Dashboard");
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            _navigationService.NavigateTo("Settings");
            return;
        }

        var item = sender.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (string)i.Content == (string)args.InvokedItem);
        if (item != null && item.Tag != null)
        {
            _navigationService.NavigateTo(item.Tag.ToString()!);
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        _navigationService.GoBack();
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        NavView.IsBackEnabled = ContentFrame.CanGoBack;
    }
}
