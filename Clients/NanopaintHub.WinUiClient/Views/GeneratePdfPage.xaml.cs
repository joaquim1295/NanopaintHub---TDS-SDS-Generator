using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class GeneratePdfPage : Page
{
    public GeneratePdfViewModel ViewModel { get; }

    public GeneratePdfPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<GeneratePdfViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.InitializeAsync();
    }

    private async void OpenPdf_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ViewModel.SavedFilePath))
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(ViewModel.SavedFilePath);
                await Windows.System.Launcher.LaunchFileAsync(file);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error launching PDF: {ex}");
            }
        }
    }
}
