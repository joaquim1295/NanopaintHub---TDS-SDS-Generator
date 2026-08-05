namespace NanopaintHub.Application.Tds;

/// <summary>Modelo plano para Handlebars (TDS sensor + SVG embutido).</summary>
public static class TdsTemplateViewModelBuilder
{
    public static object Build(
        string productName,
        string productCode,
        string category,
        string productType,
        string description,
        string revisionDate,
        int version,
        IReadOnlyList<string> features,
        IReadOnlyDictionary<string, string> liquidProperties,
        IReadOnlyDictionary<string, string> mechanicalProperties,
        IReadOnlyDictionary<string, string> functionalProperties,
        TdsSensorSpecs specs,
        IReadOnlyDictionary<string, string> chartSvgs,
        string? batchNumber,
        string? logoBase64 = null)
    {
        var unit = string.Equals(specs.ForceUnit, "Pa", StringComparison.OrdinalIgnoreCase) ? "Pa" : "N";
        var tempMinF = specs.OperatingTempMinC.HasValue
            ? CelsiusToFahrenheit(specs.OperatingTempMinC.Value)
            : (double?)null;
        var tempMaxF = specs.OperatingTempMaxC.HasValue
            ? CelsiusToFahrenheit(specs.OperatingTempMaxC.Value)
            : (double?)null;

        var hasSpecs = specs.IsEnabled && (specs.SensitivityVolts.HasValue
            || specs.SensitivityPerUnit.HasValue
            || specs.ResponseTimeSec.HasValue
            || specs.RestResistanceOhms.HasValue
            || specs.RangeMax.HasValue);

        object? sensor = specs.IsEnabled ? new
        {
            ForceUnit = unit,
            SensitivityVolts = FormatNullable(specs.SensitivityVolts),
            SensitivityPerUnit = FormatNullable(specs.SensitivityPerUnit),
            SensitivityForce = FormatNullable(specs.SensitivityPerUnit),
            SensitivityLabel = unit == "Pa" ? "V/Pa" : "V/N",
            ResponseTimeSec = FormatNullable(specs.ResponseTimeSec),
            RecoveryTimeSec = FormatNullable(specs.RecoveryTimeSec),
            RepeatabilityPercent = FormatNullable(specs.RepeatabilityPercent),
            HasRepeatability = specs.RepeatabilityPercent.HasValue,
            RestResistanceOhms = FormatNullable(specs.RestResistanceOhms),
            OperatingTempMinC = FormatNullable(specs.OperatingTempMinC),
            OperatingTempMaxC = FormatNullable(specs.OperatingTempMaxC),
            OperatingTempMinF = FormatNullable(tempMinF),
            OperatingTempMaxF = FormatNullable(tempMaxF),
            RangeMin = FormatNullable(specs.RangeMin),
            RangeMax = FormatNullable(specs.RangeMax),
            HysteresisPercent = FormatNullable(
                specs.HysteresisPercent ?? specs.Charts.Hysteresis?.Percent),
            HasHysteresisScalar = specs.HysteresisPercent.HasValue
                || specs.Charts.Hysteresis?.Percent.HasValue == true,
            DriftIntervals = specs.DriftIntervals,
            HasDrift = specs.DriftIntervals.Count > 0,
            HasSpecs = hasSpecs,
            IsEnabled = specs.IsEnabled,
        } : null;

        return new
        {
            Name = productName,
            Code = productCode,
            Category = category,
            ProductType = productType,
            Description = description,
            RevisionDate = revisionDate,
            Version = version,
            Features = features,
            LiquidProperties = liquidProperties,
            MechanicalProperties = mechanicalProperties,
            FunctionalProperties = functionalProperties,
            DocumentKind = "TDS",
            BatchNumber = batchNumber,
            HasBatch = !string.IsNullOrWhiteSpace(batchNumber),
            Sensor = sensor,
            Tds = sensor,
            Placeholders = specs.PlaceholderValues,
            Assets = MergeAssets(specs.TemplateAssets, logoBase64),
            ExtraSpecs = specs.ExtraSpecs,
            Charts = new
            {
                HasLinearity = chartSvgs.ContainsKey("linearity"),
                HasResistance = chartSvgs.ContainsKey("resistance"),
                HasHysteresis = chartSvgs.ContainsKey("hysteresis"),
                LinearitySvg = chartSvgs.GetValueOrDefault("linearity"),
                ResistanceSvg = chartSvgs.GetValueOrDefault("resistance"),
                HysteresisSvg = chartSvgs.GetValueOrDefault("hysteresis"),
                ExtraSeries = specs.Charts.ExtraSeries,
            },
        };
    }

    private static Dictionary<string, object> MergeAssets(
        IReadOnlyDictionary<string, string>? templateAssets,
        string? logoBase64)
    {
        var assets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (templateAssets is not null)
        {
            foreach (var kv in templateAssets)
            {
                assets[kv.Key] = kv.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(logoBase64) && !assets.ContainsKey("Logo"))
        {
            assets["Logo"] = logoBase64;
        }

        return assets;
    }

    private static string? FormatNullable(double? value) =>
        value.HasValue ? value.Value.ToString("G4") : null;

    private static double CelsiusToFahrenheit(double c) => c * 9.0 / 5.0 + 32.0;
}
