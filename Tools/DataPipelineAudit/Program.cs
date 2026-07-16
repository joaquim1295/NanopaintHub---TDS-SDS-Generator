using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Infrastructure.LocalData.Persistence;

class Program
{
    static void Main()
    {
        var dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub", "nanopaint.db");
        var options = new DbContextOptionsBuilder<NanopaintHubDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        using var dbContext = new NanopaintHubDbContext(options);
        
        Console.WriteLine($"Total EchaSubstances: {dbContext.EchaSubstances.Count()}");
        Console.WriteLine($"Total ClpPhrases: {dbContext.Set<NanopaintHub.Domain.Entities.ClpPhrase>().Count()}");
        Console.WriteLine($"Total SubstanceHazards: {dbContext.Set<NanopaintHub.Domain.Entities.SubstanceHazard>().Count()}");
    }
}
