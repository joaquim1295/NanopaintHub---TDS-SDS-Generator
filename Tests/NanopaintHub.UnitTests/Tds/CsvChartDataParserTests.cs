using System.IO;
using NanopaintHub.Application.Tds;
using Xunit;

namespace NanopaintHub.UnitTests.Tds;

public sealed class CsvChartDataParserTests
{
    [Fact]
    public void ParseXYPoints_StandardCsv_ReturnsParsedPoints()
    {
        var csv = """
            # Header line
            Force, Voltage
            0, 0.0
            10, 0.87
            20, 1.74
            30, 2.61
            """;

        var points = CsvChartDataParser.ParseXYPoints(csv);

        Assert.Equal(4, points.Count);
        Assert.Equal(0, points[0].X);
        Assert.Equal(0, points[0].Y);
        Assert.Equal(10, points[1].X);
        Assert.Equal(0.87, points[1].Y);
        Assert.Equal(30, points[3].X);
        Assert.Equal(2.61, points[3].Y);
    }

    [Fact]
    public void ParseXYPoints_SemicolonAndCommaDecimal_ReturnsParsedPoints()
    {
        var csv = """
            Forca; Tensao
            0; 0,0
            10,5; 0,92
            25,0; 2,15
            """;

        var points = CsvChartDataParser.ParseXYPoints(csv);

        Assert.Equal(3, points.Count);
        Assert.Equal(0, points[0].X);
        Assert.Equal(10.5, points[1].X);
        Assert.Equal(0.92, points[1].Y);
        Assert.Equal(25.0, points[2].X);
        Assert.Equal(2.15, points[2].Y);
    }

    [Fact]
    public void ParseHysteresisChart_3Columns_ReturnsLoadingAndUnloading()
    {
        var csv = """
            Force; Loading; Unloading
            0; 0; 0
            10; 100; 80
            20; 200; 170
            30; 300; 290
            """;

        var chart = CsvChartDataParser.ParseHysteresisChart(csv);

        Assert.Equal(4, chart.Loading.Count);
        Assert.Equal(4, chart.Unloading.Count);
        Assert.Equal(100, chart.Loading[1].Y);
        Assert.Equal(80, chart.Unloading[1].Y);
        Assert.NotNull(chart.Percent);
        Assert.True(chart.Percent > 0);
    }

    [Fact]
    public void ParseXYPointsFromFile_NonExistentFile_ReturnsEmptyList()
    {
        var points = CsvChartDataParser.ParseXYPointsFromFile("non_existent_file.csv");
        Assert.Empty(points);
    }
}
