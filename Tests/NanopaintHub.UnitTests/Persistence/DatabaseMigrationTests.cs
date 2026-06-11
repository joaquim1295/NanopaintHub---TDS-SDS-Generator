using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Infrastructure.LocalData.Persistence;

namespace NanopaintHub.UnitTests.Persistence;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task MigrateAsync_ProductsTable_HasTdsSensorSpecsJsonColumn()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<NanopaintHubDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new NanopaintHubDbContext(options);
        await db.Database.MigrateAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(Products);";

        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        Assert.Contains("TdsSensorSpecsJson", columns);
    }
}
