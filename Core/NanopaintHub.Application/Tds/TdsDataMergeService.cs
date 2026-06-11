using System.Text.Json;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Tds;

/// <summary>Merge produto + lote opcional para geração TDS.</summary>
public sealed class TdsDataMergeService
{
    public TdsSensorSpecs Merge(Product product, TestBatch? batch)
    {
        var merged = TdsSensorDataMerger.ParseProductSpecs(product.TdsSensorSpecsJson);
        if (batch is null || string.IsNullOrWhiteSpace(batch.TestValues))
        {
            return merged;
        }

        var overlay = TdsSensorDataMerger.ParseBatchOverlay(batch.TestValues);
        if (overlay is not null)
        {
            merged = TdsSensorDataMerger.Merge(merged, overlay);
        }

        if (LooksLikePartialSpecs(batch.TestValues))
        {
            var partial = TdsSensorDataMerger.ParseProductSpecs(batch.TestValues);
            merged = TdsSensorDataMerger.DeepMerge(merged, partial);
        }

        return merged;
    }

    public string? ResolveBatchNumber(TestBatch? batch)
    {
        if (batch is null)
        {
            return null;
        }

        var overlay = TdsSensorDataMerger.ParseBatchOverlay(batch.TestValues);
        return overlay?.BatchNumber ?? batch.BatchNumber;
    }

    private static bool LooksLikePartialSpecs(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (doc.RootElement.TryGetProperty("specOverrides", out _) ||
                doc.RootElement.TryGetProperty("chartOverrides", out _))
            {
                return false;
            }

            return doc.RootElement.TryGetProperty("forceUnit", out _) ||
                   doc.RootElement.TryGetProperty("charts", out _) ||
                   doc.RootElement.TryGetProperty("sensitivityVolts", out _) ||
                   doc.RootElement.TryGetProperty("hysteresisPercent", out _) ||
                   doc.RootElement.TryGetProperty("rangeMax", out _) ||
                   doc.RootElement.TryGetProperty("repeatabilityPercent", out _) ||
                   doc.RootElement.TryGetProperty("schemaVersion", out _) ||
                   doc.RootElement.TryGetProperty("extraSpecs", out _) ||
                   doc.RootElement.TryGetProperty("templateAssets", out _) ||
                   doc.RootElement.TryGetProperty("placeholderValues", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
