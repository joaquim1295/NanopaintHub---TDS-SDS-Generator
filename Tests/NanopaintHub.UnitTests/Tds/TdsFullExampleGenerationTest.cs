using System.Globalization;
using System.Text;
using HandlebarsDotNet;
using NanopaintHub.Application.Tds;
using NanopaintHub.Infrastructure.Documents.Tds;

namespace NanopaintHub.UnitTests.Tds;

public sealed class TdsFullExampleGenerationTest
{
    private const string OutputDir = "Output";

    [Fact]
    public void GenerateFullTdsExample()
    {
        // Arrange
        var templatePath = FindTemplatePath();
        var templateHtml = File.ReadAllText(templatePath);

        var productName = "NP-PI-1000 High Sensitivity Piezoresistive Ink";
        var productCode = "NP-PI-1000";
        var description = "High-performance carbon-based piezoresistive ink optimized for face-to-face " +
                          "printing on flexible substrates. Exhibits strong linear piezoresistive response " +
                          "with minimal hysteresis, suitable for wearable sensors and industrial force sensing.";

        var specs = new TdsSensorSpecs
        {
            ForceUnit = "N",
            SensitivityVolts = 0.82,
            SensitivityPerUnit = 0.082,
            ResponseTimeSec = 0.018,
            RecoveryTimeSec = 0.025,
            RepeatabilityPercent = 2.1,
            RestResistanceOhms = 118000,
            OperatingTempMinC = -10,
            OperatingTempMaxC = 85,
            RangeMin = 0,
            RangeMax = 50,
            HysteresisPercent = null, // vem do batch
            Charts = new TdsChartData
            {
                Linearity =
                [
                    new() { X = 0, Y = 0 },
                    new() { X = 10, Y = 98.2 },
                    new() { X = 20, Y = 100.5 },
                    new() { X = 30, Y = 103.1 },
                    new() { X = 50, Y = 112.0 },
                ],
                ResistanceVsForce =
                [
                    new() { X = 0, Y = 120000 },
                    new() { X = 5, Y = 98000 },
                    new() { X = 10, Y = 72000 },
                    new() { X = 20, Y = 41000 },
                    new() { X = 50, Y = 18500 },
                ],
                Hysteresis = new TdsHysteresisChart
                {
                    Percent = 3.8,
                    Loading =
                    [
                        new() { X = 0, Y = 0 },
                        new() { X = 5, Y = 2.1 },
                        new() { X = 10, Y = 4.0 },
                        new() { X = 20, Y = 6.2 },
                        new() { X = 30, Y = 7.5 },
                        new() { X = 50, Y = 8.8 },
                    ],
                    Unloading =
                    [
                        new() { X = 50, Y = 8.8 },
                        new() { X = 30, Y = 7.2 },
                        new() { X = 20, Y = 5.9 },
                        new() { X = 10, Y = 3.8 },
                        new() { X = 5, Y = 1.9 },
                        new() { X = 0, Y = 0 },
                    ],
                },
            },
            ExtraSpecs = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Cross-sensitivity"] = "< 3 %",
                ["Long-term drift"] = "0.5 % / month",
            },
        };

        var batchOverlay = new TdsBatchOverlay
        {
            BatchNumber = "B2024-06-11-A3",
            SpecOverrides = new(StringComparer.OrdinalIgnoreCase)
            {
                ["HysteresisPercent"] = 4.2,
                ["SensitivityVolts"] = 0.87,
            },
        };

        // Act — aplicar batch merge
        var merged = TdsSensorDataMerger.Merge(specs, batchOverlay);

        // Gerar SVGs
        var renderer = new TdsChartSvgBuilder();
        var chartSvgs = renderer.RenderAll(merged);

        // Construir ViewModel
        var features = new List<string>
        {
            "Carbon-based piezoresistive",
            "Face-to-face printing compatible",
            "Flexible substrate optimized",
            "High sensitivity (GF > 10)",
            "Low hysteresis",
            "Stretchable up to 30 %",
            "RoHS compliant",
        };

        var liquidProps = new Dictionary<string, string>
        {
            ["Appearance"] = "Black viscous paste",
            ["Viscosity"] = "25–35 Pa·s (Brookfield)",
            ["Solvent content"] = "< 5 %",
            ["Density"] = "1.15 g/cm³",
            ["Shelf life"] = "6 months (2–8 °C)",
        };

        var mechProps = new Dictionary<string, string>
        {
            ["Substrate"] = "PET, PI, TPU",
            ["Curing"] = "120 °C / 15 min",
            ["Dry film thickness"] = "15–25 µm",
            ["Adhesion"] = "5B (ASTM D3359)",
        };

        var funcProps = new Dictionary<string, string>
        {
            ["Resistance range"] = "10 kΩ – 500 kΩ",
            ["Gauge factor (GF)"] = "12–18",
            ["Max strain"] = "30 %",
            ["Fatigue life"] = "> 1M cycles @ 10 %",
        };

        var logoBase64 = LoadLogoBase64();
        var templateData = TdsTemplateViewModelBuilder.Build(
            productName,
            productCode,
            description,
            "11/06/2026",
            3,
            features,
            liquidProps,
            mechProps,
            funcProps,
            merged,
            chartSvgs,
            batchOverlay.BatchNumber,
            logoBase64);

        var compiled = Handlebars.Compile(templateHtml);
        var html = compiled(templateData);

        // Guardar output
        Directory.CreateDirectory(OutputDir);
        var outputPath = Path.Combine(OutputDir, "tds-full-example.html");
        File.WriteAllText(outputPath, html, Encoding.UTF8);

        Assert.NotEmpty(html);
        Assert.Contains("<svg", html, StringComparison.Ordinal);
        Assert.Contains("Linearidade", html, StringComparison.Ordinal);
        Assert.Contains("Resistance", html, StringComparison.Ordinal);
        Assert.Contains("Histerese", html, StringComparison.Ordinal);
        Assert.Contains("B2024-06-11-A3", html, StringComparison.Ordinal);
        Assert.Contains("4.2", html, StringComparison.Ordinal);
    }

    private static string FindTemplatePath()
    {
        var probe = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(probe);

        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "Assets", "Templates", "MinimalTds.html");

            if (File.Exists(candidate))
                return candidate;

            // Tentar pelo caminho relativo ao projeto
            candidate = Path.Combine(
                dir.FullName,
                "Clients", "NanopaintHub.WinUiClient",
                "Assets", "Templates", "MinimalTds.html");

            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "MinimalTds.html not found. Ensure the template exists in the WinUiClient Assets/Templates folder.");
    }

    private static string? LoadLogoBase64()
    {
        // Procurar o logo nos mesmos locais que o template
        var probe = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(probe);

        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "Assets", "Nanopaint_logo.png");

            if (File.Exists(candidate))
                return ImageToBase64(candidate);

            candidate = Path.Combine(
                dir.FullName,
                "Clients", "NanopaintHub.WinUiClient",
                "Assets", "Nanopaint_logo.png");

            if (File.Exists(candidate))
                return ImageToBase64(candidate);

            dir = dir.Parent;
        }

        return null;
    }

    private static string ImageToBase64(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/png",
        };
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}
