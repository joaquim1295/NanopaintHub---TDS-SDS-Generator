using Microsoft.UI.Xaml;
using NanopaintHub_WinUIClient.Views;

namespace NanopaintHub_WinUIClient;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        ExtendsContentIntoTitleBar = true;
        
        RootFrame.Navigate(typeof(ShellPage));
    }
}
