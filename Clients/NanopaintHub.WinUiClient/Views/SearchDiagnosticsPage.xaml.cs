using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class SearchDiagnosticsPage : Page
{
    public SearchDiagnosticsViewModel ViewModel { get; }

    public SearchDiagnosticsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SearchDiagnosticsViewModel>();
        DataContext = ViewModel;
    }
}
