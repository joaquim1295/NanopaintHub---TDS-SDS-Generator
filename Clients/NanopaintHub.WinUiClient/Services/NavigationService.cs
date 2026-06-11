using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace NanopaintHub_WinUIClient.Services;

public interface INavigationService
{
    void Initialize(Frame shellFrame);
    bool NavigateTo(string pageKey, object? parameter = null);
    bool GoBack();
    event NavigatedEventHandler? Navigated;
}

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public event NavigatedEventHandler? Navigated;

    public void Initialize(Frame shellFrame)
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }

        _frame = shellFrame;
        _frame.Navigated += OnNavigated;
    }

    public bool NavigateTo(string pageKey, object? parameter = null)
    {
        if (_frame == null) return false;

        var pageType = Type.GetType($"NanopaintHub_WinUIClient.Views.{pageKey}Page");
        if (pageType == null) return false;

        return _frame.Navigate(pageType, parameter);
    }

    public bool GoBack()
    {
        if (_frame != null && _frame.CanGoBack)
        {
            _frame.GoBack();
            return true;
        }
        return false;
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        Navigated?.Invoke(sender, e);
    }
}
