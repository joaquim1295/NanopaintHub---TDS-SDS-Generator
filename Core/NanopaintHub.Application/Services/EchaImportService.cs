namespace NanopaintHub.Application.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using Microsoft.Extensions.DependencyInjection;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

public class EchaImportService : IEchaImportService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EchaImportService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<int> ImportCsvAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("O ficheiro CSV não foi encontrado.", filePath);

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var records = new List<EchaSubstance>();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".csv")
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";", // Normalmente os ficheiros europeus usam ';' ou '\t'
                BadDataFound = null,
                MissingFieldFound = null
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                await csv.ReadAsync();
                csv.ReadHeader();

                while (await csv.ReadAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cas = GetFieldValue(csv, "CAS Number", "CAS No", "CAS", "Cas number", "Cas No", "cas");
                    var ec = GetFieldValue(csv, "EC Number", "EC No", "EC", "Ec number", "Ec No", "ec");
                    var name = GetFieldValue(csv, "International Chemical Identification", "International chemical identification", "Chemical Name", "Chemical name", "Substance Name", "Substance name", "Name", "name");
                    var hazardsStr = GetFieldValue(csv, "Hazard Statement Code(s)", "Hazard statement code(s)", "Hazards", "H-phrases", "Classification", "classification");

                    ProcessRow(cas, ec, name, hazardsStr, records);
                }
            }
        }
        else if (extension == ".xls" || extension == ".xlsx")
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                if (result.Tables.Count > 0)
                {
                    var table = result.Tables[0];
                    var casCol = GetColumnIndex(table, "CAS Number", "CAS No", "CAS", "Cas number", "Cas No", "cas");
                    var ecCol = GetColumnIndex(table, "EC Number", "EC No", "EC", "Ec number", "Ec No", "ec");
                    var nameCol = GetColumnIndex(table, "International Chemical Identification", "International chemical identification", "Chemical Name", "Chemical name", "Substance Name", "Substance name", "Name", "name");
                    var hazardsCol = GetColumnIndex(table, "Hazard Statement Code(s)", "Hazard statement code(s)", "Hazards", "H-phrases", "Classification", "classification");

                    foreach (System.Data.DataRow row in table.Rows)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var cas = casCol >= 0 ? row[casCol]?.ToString() : null;
                        var ec = ecCol >= 0 ? row[ecCol]?.ToString() : null;
                        var name = nameCol >= 0 ? row[nameCol]?.ToString() : null;
                        var hazardsStr = hazardsCol >= 0 ? row[hazardsCol]?.ToString() : null;

                        ProcessRow(cas, ec, name, hazardsStr, records);
                    }
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Formato não suportado: {extension}");
        }
        var count = 0;
        
        // Chunk records into batches of 1000
        var batchSize = 1000;
        var batches = records
            .Where(r => !string.IsNullOrWhiteSpace(r.CasNumber))
            .GroupBy(r => r.CasNumber) // Evitar duplicados no mesmo lote
            .Select(g => g.First())
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / batchSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            using var scope = _scopeFactory.CreateScope();
            var echaRepository = scope.ServiceProvider.GetRequiredService<IEchaSubstanceRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var casNumbers = batch.Select(r => r.CasNumber).ToList();
            var existingCasNumbers = await echaRepository.GetExistingCasNumbersAsync(casNumbers, cancellationToken);
            var existingSet = new HashSet<string>(existingCasNumbers, StringComparer.OrdinalIgnoreCase);

            var addedInBatch = 0;
            foreach (var r in batch)
            {
                if (!existingSet.Contains(r.CasNumber))
                {
                    await echaRepository.AddAsync(r, cancellationToken);
                    addedInBatch++;
                    count++;
                }
            }

            if (addedInBatch > 0)
            {
                await unitOfWork.CommitAsync();
            }
        }

        return count;
    }

    private string? GetFieldValue(CsvReader csv, params string[] possibleHeaders)
    {
        foreach (var header in possibleHeaders)
        {
            if (csv.TryGetField<string>(header, out var value))
            {
                return value;
            }
        }
        return null;
    }

    private int GetColumnIndex(System.Data.DataTable table, params string[] possibleHeaders)
    {
        foreach (var header in possibleHeaders)
        {
            if (table.Columns.Contains(header))
                return table.Columns.IndexOf(header);
        }
        return -1;
    }

    private void ProcessRow(string? cas, string? ec, string? name, string? hazardsStr, List<EchaSubstance> records)
    {
        if (string.IsNullOrWhiteSpace(cas) && string.IsNullOrWhiteSpace(name))
            return;

        var substance = new EchaSubstance
        {
            CasNumber = cas?.Trim() ?? string.Empty,
            EcNumber = ec?.Trim() ?? string.Empty,
            ChemicalName = name?.Trim() ?? "Unknown Substance"
        };

        if (!string.IsNullOrWhiteSpace(hazardsStr))
        {
            var codes = hazardsStr.Split(new[] { ',', ';', '|', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var code in codes.Where(c => c.StartsWith("H")))
            {
                substance.Hazards.Add(new SubstanceHazard 
                { 
                    Substance = substance,
                    PhraseCode = code.Trim()
                });
            }
        }

        records.Add(substance);
    }
}
