using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.Services;
using Microsoft.UI.Xaml.Controls;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isBackEnabled;

    [ObservableProperty]
    private NavigationViewItem? _selectedItem;

    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            IsBackEnabled = frame.CanGoBack;
        }
    }
}
