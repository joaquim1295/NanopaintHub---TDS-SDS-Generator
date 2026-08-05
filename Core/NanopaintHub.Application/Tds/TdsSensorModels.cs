using System.Text.Json.Serialization;

namespace NanopaintHub.Application.Tds;

/// <summary>Especificações TDS de sensor (produto) e curvas para gráficos no PDF.</summary>
public sealed class TdsSensorSpecs
{
    public int SchemaVersion { get; set; } = 2;

    public bool IsEnabled { get; set; } = true;

    public string ForceUnit { get; set; } = "N";

    public double? SensitivityVolts { get; set; }

    [JsonPropertyName("sensitivityPerUnit")]
    public double? SensitivityPerUnit { get; set; }

    [JsonPropertyName("sensitivityForce")]
    public double? SensitivityForce
    {
        get => SensitivityPerUnit;
        set => SensitivityPerUnit = value;
    }

    public double? ResponseTimeSec { get; set; }

    public double? RecoveryTimeSec { get; set; }

    public double? RepeatabilityPercent { get; set; }

    public double? RestResistanceOhms { get; set; }

    public double? OperatingTempMinC { get; set; }

    public double? OperatingTempMaxC { get; set; }

    public double? RangeMin { get; set; }

    public double? RangeMax { get; set; }

    public double? HysteresisPercent { get; set; }

    /// <summary>Intervalos de drift seleccionados (máx. 3): "1m", "10m", "30m", "1h". Fase futura.</summary>
    public List<string> DriftIntervals { get; set; } = [];

    public TdsChartData Charts { get; set; } = new();

    /// <summary>Campos adicionais para evolução sem migração de tabela (JSON estendido).</summary>
    public Dictionary<string, string> ExtraSpecs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>URLs/caminhos de assets dinâmicos (logo, foto, ícones, etc.) para o template.</summary>
    public Dictionary<string, string> TemplateAssets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Marcadores de layout livres para placeholders dinâmicos do builder visual.</summary>
    public Dictionary<string, string> PlaceholderValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TdsChartData
{
    public List<TdsChartPoint> Linearity { get; set; } = [];

    public List<TdsChartPoint> ResistanceVsForce { get; set; } = [];

    [JsonPropertyName("resistanceVsOhms")]
    public List<TdsChartPoint> ResistanceVsOhms
    {
        get => ResistanceVsForce;
        set => ResistanceVsForce = value;
    }

    public TdsHysteresisChart? Hysteresis { get; set; }

    /// <summary>Coleção para curvas adicionais fora das 3 curvas padrão.</summary>
    public Dictionary<string, List<TdsChartPoint>> ExtraSeries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TdsChartPoint
{
    public double X { get; set; }

    public double Y { get; set; }
}

public sealed class TdsHysteresisChart
{
    public double? Percent { get; set; }

    public List<TdsChartPoint> Loading { get; set; } = [];

    public List<TdsChartPoint> Unloading { get; set; } = [];
}

/// <summary>Overrides opcionais vindos de um lote de teste (TestBatch.TestValues).</summary>
public sealed class TdsBatchOverlay
{
    public int? SchemaVersion { get; set; }

    public Dictionary<string, object?> SpecOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public TdsChartData? ChartOverrides { get; set; }

    public string? BatchNumber { get; set; }

    public Dictionary<string, string>? PlaceholderOverrides { get; set; }

    public Dictionary<string, string>? AssetOverrides { get; set; }
}
