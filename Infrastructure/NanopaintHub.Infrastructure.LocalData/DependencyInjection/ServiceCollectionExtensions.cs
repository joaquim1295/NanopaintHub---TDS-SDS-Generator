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
            options.UseSqlite(sqliteConnectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierComponentRepository, SupplierComponentRepository>();
        services.AddScoped<ITestBatchRepository, TestBatchRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
