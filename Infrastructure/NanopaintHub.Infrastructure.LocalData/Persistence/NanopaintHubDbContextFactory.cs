namespace NanopaintHub.Infrastructure.LocalData.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class NanopaintHubDbContextFactory : IDesignTimeDbContextFactory<NanopaintHubDbContext>
{
    public NanopaintHubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NanopaintHubDbContext>();
        
        // Diz ao EF Core para criar o ficheiro SQLite na pasta do projeto
        optionsBuilder.UseSqlite("Data Source=nanopaint.db");

        return new NanopaintHubDbContext(optionsBuilder.Options);
    }
}