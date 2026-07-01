using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using NanopaintHub.Infrastructure.LocalData.Persistence;
using NanopaintHub_WinUIClient.Services;
using NanopaintHub_WinUIClient.ViewModels;
using NanopaintHub.Application.Features.TdsGeneration;
using NanopaintHub.Application.Features.SdsGeneration;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Services;

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

        builder.Configuration.AddJsonFile(Path.Combine(dataDir, "appsettings.user.json"), optional: true, reloadOnChange: true);

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        // Register Core/Infrastructure Services
        builder.Services.AddNanopaintHubPersistence(connectionString);
        builder.Services.AddNanopaintHubDocuments(builder.Configuration);

        // Register App Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IHtmlToPdfGenerator, NanopaintHub.Infrastructure.Documents.Services.PuppeteerHtmlToPdfGenerator>();
builder.Services.AddSingleton<PdfPreviewService>();
        // Register ViewModels
        builder.Services.AddTransient<ShellViewModel>();
        builder.Services.AddTransient<ProductsViewModel>();
        builder.Services.AddTransient<ProductEditorViewModel>();
        builder.Services.AddTransient<TemplatesViewModel>();
        builder.Services.AddTransient<TemplateEditorViewModel>();
        builder.Services.AddTransient<GeneratePdfViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Register Use Cases and Services
        builder.Services.AddTransient<GenerateTdsUseCase>();
        builder.Services.AddTransient<GenerateSdsUseCase>();
        builder.Services.AddTransient<IEchaImportService, EchaImportService>();
        builder.Services.AddTransient<ISubstanceSearchService, SubstanceSearchService>();

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
