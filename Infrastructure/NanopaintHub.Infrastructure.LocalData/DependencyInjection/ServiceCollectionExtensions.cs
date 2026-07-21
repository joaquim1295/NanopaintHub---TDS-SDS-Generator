namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Infrastructure.LocalData.Persistence;
using NanopaintHub.Infrastructure.LocalData.Repositories;

public static class NanopaintHubPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Regista EF Core SQLite, repositórios e Unit of Work.
    /// </summary>
    public static IServiceCollection AddNanopaintHubPersistence(
        this IServiceCollection services,
        string sqliteConnectionString)
    {
        services.AddDbContext<NanopaintHubDbContext>(options =>
        {
            options.UseSqlite(sqliteConnectionString);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IClpPhraseRepository, ClpPhraseRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITestBatchRepository, TestBatchRepository>();
        services.AddScoped<IDocumentReleaseRepository, DocumentReleaseRepository>();
        services.AddScoped<IEchaSubstanceRepository, EchaSubstanceRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISupplierProductRepository, SupplierProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // SDS Compliance Engine
        services.AddScoped<IClpPhraseTranslationRepository, ClpPhraseTranslationRepository>();
        services.AddScoped<NanopaintHub.Application.Contracts.ISdsHazardAggregatorService,
                           NanopaintHub.Application.Services.SdsHazardAggregatorService>();

        return services;
    }
}
