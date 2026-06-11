using NanopaintHub.Application.Tds;
using NanopaintHub.Infrastructure.Documents.Tds;

namespace NanopaintHub.UnitTests.Tds;

public sealed class TdsChartSvgBuilderTests
{
    [Fact]
    public void RenderAll_WithLinearityPoints_ProducesSvg()
    {
        var specs = new TdsSensorSpecs
        {
            ForceUnit = "N",
            Charts = new TdsChartData
            {
                Linearity =
                [
                    new TdsChartPoint { X = 0, Y = 0 },
                    new TdsChartPoint { X = 10, Y = 9.8 },
                ],
            },
        };

        var renderer = new TdsChartSvgBuilder();
        var charts = renderer.RenderAll(specs);

        Assert.True(charts.ContainsKey("linearity"));
        Assert.Contains("<svg", charts["linearity"], StringComparison.Ordinal);
        Assert.Contains("Linearidade", charts["linearity"], StringComparison.Ordinal);
    }
}
