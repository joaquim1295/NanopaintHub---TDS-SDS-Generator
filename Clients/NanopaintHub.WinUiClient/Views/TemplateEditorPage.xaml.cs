using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient.Views;

public sealed partial class TemplateEditorPage : Page
{
    public TemplateEditorViewModel ViewModel { get; }

    public TemplateEditorPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<TemplateEditorViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Guid? templateId = null;
        if (e.Parameter is Guid id)
        {
            templateId = id;
        }
        
        _ = ViewModel.InitializeAsync(templateId);
    }
}
