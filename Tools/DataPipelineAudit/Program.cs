using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Infrastructure.LocalData.Persistence;
using NanopaintHub.Infrastructure.LocalData.Repositories;

namespace DataPipelineAudit
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Data Pipeline Audit for ECHA Substances...");
            
            var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub");
            var dbPath = Path.Combine(dataDir, "nanopaint.db");
            var connectionString = $"Data Source={dbPath}";
            
            var options = new DbContextOptionsBuilder<NanopaintHubDbContext>()
                .UseSqlite(connectionString)
                .Options;

            using var dbContext = new NanopaintHubDbContext(options);
            var repository = new EchaSubstanceRepository(dbContext);

            Console.WriteLine("Querying for 'acetone'...");
            try
            {
                var results = await repository.SearchByNameAsync("acetone");
                Console.WriteLine($"Found {results.Count} results.");

                foreach (var r in results)
                {
                    Console.WriteLine($"ID: {r.Id}, Name: {r.ChemicalName}, CAS: {r.CasNumber}");
                    if (r.Hazards != null)
                    {
                        Console.WriteLine($"  Hazards ({r.Hazards.Count}):");
                        foreach (var h in r.Hazards)
                        {
                            Console.WriteLine($"    - {h.PhraseCode} (Phrase: {h.Phrase?.Code})");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  Hazards: NULL");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRASH! Exception during SearchByNameAsync:");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
