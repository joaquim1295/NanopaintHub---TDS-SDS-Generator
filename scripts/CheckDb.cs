using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NanopaintHub");
        var dbPath = Path.Combine(dataDir, "nanopaint.db");
        var connectionString = $"Data Source={dbPath}";

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT sp.Name as SupplierProduct, spc.Percentage, es.ChemicalName 
            FROM SupplierProducts sp
            LEFT JOIN SupplierProductCompositions spc ON sp.Id = spc.SupplierProductId
            LEFT JOIN EchaSubstances es ON spc.EchaSubstanceId = es.Id
        ";
        
        using var reader = cmd.ExecuteReader();
        Console.WriteLine("Supplier Products and Compositions:");
        while (reader.Read())
        {
            var spName = reader.GetString(0);
            var pct = reader.IsDBNull(1) ? "NULL" : reader.GetDouble(1).ToString();
            var chemName = reader.IsDBNull(2) ? "NULL" : reader.GetString(2);
            Console.WriteLine($"- {spName}: {pct}% of {chemName}");
        }
    }
}
