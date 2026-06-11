namespace NanopaintHub.Application.Tds;

public static class TdsSensorSpecsValidator
{
    private static readonly HashSet<string> AllowedDrift = new(StringComparer.OrdinalIgnoreCase)
    {
        "1m", "10m", "30m", "1h",
    };

    public static void Validate(TdsSensorSpecs specs)
    {
        if (specs.SchemaVersion < 1)
        {
            throw new InvalidOperationException("SchemaVersion inválido para especificações TDS.");
        }

        if (!string.Equals(specs.ForceUnit, "N", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(specs.ForceUnit, "Pa", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Unidade de força inválida. Use N ou Pa.");
        }

        if (specs.DriftIntervals.Count > 3)
        {
            throw new InvalidOperationException("Seleccione no máximo 3 intervalos de drift.");
        }

        foreach (var interval in specs.DriftIntervals)
        {
            if (!AllowedDrift.Contains(interval))
            {
                throw new InvalidOperationException(
                    $"Intervalo de drift inválido: «{interval}». Use 1m, 10m, 30m ou 1h.");
            }
        }

        if (specs.OperatingTempMinC.HasValue && specs.OperatingTempMaxC.HasValue &&
            specs.OperatingTempMinC > specs.OperatingTempMaxC)
        {
            throw new InvalidOperationException("Temperatura mínima não pode exceder a máxima.");
        }

        if (specs.RangeMin.HasValue && specs.RangeMax.HasValue && specs.RangeMin > specs.RangeMax)
        {
            throw new InvalidOperationException("Range mínimo não pode exceder o máximo.");
        }

        if (specs.ExtraSpecs.Any(x => string.IsNullOrWhiteSpace(x.Key)))
        {
            throw new InvalidOperationException("ExtraSpecs contém chaves vazias.");
        }

        if (specs.PlaceholderValues.Any(x => string.IsNullOrWhiteSpace(x.Key)))
        {
            throw new InvalidOperationException("PlaceholderValues contém chaves vazias.");
        }

        if (specs.TemplateAssets.Any(x => string.IsNullOrWhiteSpace(x.Key)))
        {
            throw new InvalidOperationException("TemplateAssets contém chaves vazias.");
        }
    }
}
