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
}
