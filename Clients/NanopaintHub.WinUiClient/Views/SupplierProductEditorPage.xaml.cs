using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class SupplierProductEditorPage : Page
{
    public SupplierProductEditorViewModel ViewModel { get; }

    public SupplierProductEditorPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SupplierProductEditorViewModel>();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Guid? productId = null;
        if (e.Parameter is Guid id)
        {
            productId = id;
        }

        await ViewModel.InitializeAsync(productId);
    }

    private string GetOperator(string? percentageText)
    {
        if (string.IsNullOrWhiteSpace(percentageText))
            return string.Empty;

        var match = System.Text.RegularExpressions.Regex.Match(percentageText, @"^\s*(>=|<=|>|<)\s*");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }


}
