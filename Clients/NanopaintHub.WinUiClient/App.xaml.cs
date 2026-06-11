using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using NanopaintHub.Infrastructure.LocalData.Persistence;
using NanopaintHub_WinUIClient.Services;
using NanopaintHub_WinUIClient.ViewModels;

namespace NanopaintHub_WinUIClient;

public partial class App : Application
{
    private IHost? _host;

    public Microsoft.UI.Xaml.Window? MainWindow { get; set; }

    public static IServiceProvider Services =>
        Current is App app && app._host is not null
            ? app._host.Services
            : throw new InvalidOperationException("Serviços ainda não estão disponíveis.");

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NanopaintHub");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "nanopaint.db");
        var connectionString = $"Data Source={dbPath}";

        var builder = Host.CreateApplicationBuilder();

        // Register Core/Infrastructure Services
        builder.Services.AddNanopaintHubPersistence(connectionString);
        builder.Services.AddNanopaintHubDocuments();

        // Register App Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // Register ViewModels
        builder.Services.AddTransient<ShellViewModel>();
        builder.Services.AddTransient<ProductsViewModel>();
        builder.Services.AddTransient<ProductEditorViewModel>();
        builder.Services.AddTransient<TemplatesViewModel>();
        builder.Services.AddTransient<TemplateEditorViewModel>();
        builder.Services.AddTransient<GeneratePdfViewModel>();

        _host = builder.Build();

        try
        {
            await DatabaseSeeder.InitializeAsync(_host.Services);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Init Error: {ex}");
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Activate();
    }
}
