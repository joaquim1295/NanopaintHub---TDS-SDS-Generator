using System.Text.Json;
using System.Text.Json.Serialization;

namespace NanopaintHub.Application.Tds;

public static class TdsSensorDataMerger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static TdsSensorSpecs ParseProductSpecs(string? json) =>
        Normalize(
            string.IsNullOrWhiteSpace(json)
                ? new TdsSensorSpecs()
                : JsonSerializer.Deserialize<TdsSensorSpecs>(json, JsonOptions) ?? new TdsSensorSpecs());

    public static TdsBatchOverlay? ParseBatchOverlay(string? testValuesJson)
    {
        if (string.IsNullOrWhiteSpace(testValuesJson) || testValuesJson.Trim() == "{}")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TdsBatchOverlay>(testValuesJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string SerializeProductSpecs(TdsSensorSpecs specs)
    {
        specs.SchemaVersion = 2;
        return JsonSerializer.Serialize(specs, JsonOptions);
    }

    /// <summary>Produto como base; lote sobrepõe campos e curvas quando presentes.</summary>
    public static TdsSensorSpecs Merge(TdsSensorSpecs product, TdsBatchOverlay? batch)
    {
        if (batch is null)
        {
            return Normalize(product);
        }

        var merged = Clone(product);
        merged.SchemaVersion = 2;

        ApplySpecOverrides(merged, batch.SpecOverrides);

        if (batch.ChartOverrides is not null)
        {
            if (batch.ChartOverrides.Linearity.Count > 0)
            {
                merged.Charts.Linearity = batch.ChartOverrides.Linearity;
            }

            if (batch.ChartOverrides.ResistanceVsForce.Count > 0)
            {
                merged.Charts.ResistanceVsForce = batch.ChartOverrides.ResistanceVsForce;
            }

            if (batch.ChartOverrides.Hysteresis is not null)
            {
                merged.Charts.Hysteresis = batch.ChartOverrides.Hysteresis;
            }
        }

        if (batch.PlaceholderOverrides is not null)
        {
            foreach (var (key, value) in batch.PlaceholderOverrides)
            {
                if (!string.IsNullOrWhiteSpace(key) && value is not null)
                {
                    merged.PlaceholderValues[key] = value;
                }
            }
        }

        if (batch.AssetOverrides is not null)
        {
            foreach (var (key, value) in batch.AssetOverrides)
            {
                if (!string.IsNullOrWhiteSpace(key) && value is not null)
                {
                    merged.TemplateAssets[key] = value;
                }
            }
        }

        if (merged.DriftIntervals.Count > 3)
        {
            merged.DriftIntervals = merged.DriftIntervals.Take(3).ToList();
        }

        return Normalize(merged);
    }

    /// <summary>Merge profundo: valores do lote substituem os do produto quando definidos.</summary>
    public static TdsSensorSpecs DeepMerge(TdsSensorSpecs product, TdsSensorSpecs batchPartial)
    {
        var merged = Clone(product);
        merged.SchemaVersion = 2;

        if (!string.IsNullOrWhiteSpace(batchPartial.ForceUnit))
        {
            merged.ForceUnit = batchPartial.ForceUnit;
        }

        merged.SensitivityVolts = Coalesce(batchPartial.SensitivityVolts, merged.SensitivityVolts);
        merged.SensitivityPerUnit = Coalesce(batchPartial.SensitivityPerUnit, merged.SensitivityPerUnit);
        merged.ResponseTimeSec = Coalesce(batchPartial.ResponseTimeSec, merged.ResponseTimeSec);
        merged.RecoveryTimeSec = Coalesce(batchPartial.RecoveryTimeSec, merged.RecoveryTimeSec);
        merged.RepeatabilityPercent = Coalesce(batchPartial.RepeatabilityPercent, merged.RepeatabilityPercent);
        merged.RestResistanceOhms = Coalesce(batchPartial.RestResistanceOhms, merged.RestResistanceOhms);
        merged.OperatingTempMinC = Coalesce(batchPartial.OperatingTempMinC, merged.OperatingTempMinC);
        merged.OperatingTempMaxC = Coalesce(batchPartial.OperatingTempMaxC, merged.OperatingTempMaxC);
        merged.RangeMin = Coalesce(batchPartial.RangeMin, merged.RangeMin);
        merged.RangeMax = Coalesce(batchPartial.RangeMax, merged.RangeMax);
        merged.HysteresisPercent = Coalesce(batchPartial.HysteresisPercent, merged.HysteresisPercent);

        if (batchPartial.DriftIntervals.Count > 0)
        {
            merged.DriftIntervals = batchPartial.DriftIntervals.Take(3).ToList();
        }

        if (batchPartial.Charts.Linearity.Count > 0)
        {
            merged.Charts.Linearity = batchPartial.Charts.Linearity;
        }

        if (batchPartial.Charts.ResistanceVsForce.Count > 0)
        {
            merged.Charts.ResistanceVsForce = batchPartial.Charts.ResistanceVsForce;
        }

        if (batchPartial.Charts.Hysteresis is not null)
        {
            merged.Charts.Hysteresis = batchPartial.Charts.Hysteresis;
        }

        if (batchPartial.ExtraSpecs.Count > 0)
        {
            foreach (var (key, value) in batchPartial.ExtraSpecs)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    merged.ExtraSpecs[key] = value;
                }
            }
        }

        if (batchPartial.TemplateAssets.Count > 0)
        {
            foreach (var (key, value) in batchPartial.TemplateAssets)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    merged.TemplateAssets[key] = value;
                }
            }
        }

        if (batchPartial.PlaceholderValues.Count > 0)
        {
            foreach (var (key, value) in batchPartial.PlaceholderValues)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    merged.PlaceholderValues[key] = value;
                }
            }
        }

        if (batchPartial.Charts.ExtraSeries.Count > 0)
        {
            foreach (var (key, value) in batchPartial.Charts.ExtraSeries)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    merged.Charts.ExtraSeries[key] = value;
                }
            }
        }

        return Normalize(merged);
    }

    private static double? Coalesce(double? primary, double? fallback) => primary ?? fallback;

    private static void ApplySpecOverrides(TdsSensorSpecs target, Dictionary<string, object?> overrides)
    {
        foreach (var (key, value) in overrides)
        {
            if (value is null)
            {
                continue;
            }

            switch (key.ToLowerInvariant())
            {
                case "forceunit":
                    target.ForceUnit = value.ToString() ?? target.ForceUnit;
                    break;
                case "sensitivityvolts":
                    target.SensitivityVolts = ToDouble(value);
                    break;
                case "sensitivityperunit":
                case "sensitivityforce":
                    target.SensitivityPerUnit = ToDouble(value);
                    break;
                case "responsetimesec":
                    target.ResponseTimeSec = ToDouble(value);
                    break;
                case "recoverytimesec":
                    target.RecoveryTimeSec = ToDouble(value);
                    break;
                case "repeatabilitypercent":
                    target.RepeatabilityPercent = ToDouble(value);
                    break;
                case "restresistanceohms":
                    target.RestResistanceOhms = ToDouble(value);
                    break;
                case "operatingtempminc":
                    target.OperatingTempMinC = ToDouble(value);
                    break;
                case "operatingtempmaxc":
                    target.OperatingTempMaxC = ToDouble(value);
                    break;
                case "rangemin":
                    target.RangeMin = ToDouble(value);
                    break;
                case "rangemax":
                    target.RangeMax = ToDouble(value);
                    break;
                case "hysteresispercent":
                    target.HysteresisPercent = ToDouble(value);
                    break;
            }
        }
    }

    private static double? ToDouble(object value)
    {
        if (value is JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetDouble(),
                JsonValueKind.String when double.TryParse(el.GetString(), out var d) => d,
                _ => null,
            };
        }

        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            string s when double.TryParse(s, out var d) => d,
            _ => null,
        };
    }

    private static TdsSensorSpecs Clone(TdsSensorSpecs source) =>
        JsonSerializer.Deserialize<TdsSensorSpecs>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
        ?? new TdsSensorSpecs();

    private static TdsSensorSpecs Normalize(TdsSensorSpecs specs)
    {
        specs.SchemaVersion = specs.SchemaVersion <= 0 ? 1 : specs.SchemaVersion;
        if (specs.SchemaVersion < 2)
        {
            specs.SchemaVersion = 2;
        }

        specs.ExtraSpecs ??= new(StringComparer.OrdinalIgnoreCase);
        specs.TemplateAssets ??= new(StringComparer.OrdinalIgnoreCase);
        specs.PlaceholderValues ??= new(StringComparer.OrdinalIgnoreCase);
        specs.Charts ??= new TdsChartData();
        specs.Charts.ExtraSeries ??= new(StringComparer.OrdinalIgnoreCase);
        return specs;
    }
}
