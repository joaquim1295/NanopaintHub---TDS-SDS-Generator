using NanopaintHub.Application.Tds;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.UnitTests.Tds;

public sealed class TdsSensorDataMergerTests
{
    [Fact]
    public void Merge_BatchOverlay_UpdatesRepeatabilityOnly()
    {
        var product = new TdsSensorSpecs
        {
            ForceUnit = "N",
            RangeMin = 0,
            RangeMax = 500,
            RepeatabilityPercent = 1.0,
        };

        var overlay = new TdsBatchOverlay
        {
            SpecOverrides = new Dictionary<string, object?> { ["repeatabilityPercent"] = 0.4 },
        };

        var merged = TdsSensorDataMerger.Merge(product, overlay);

        Assert.Equal(0.4, merged.RepeatabilityPercent);
        Assert.Equal(500, merged.RangeMax);
    }

    [Fact]
    public void DeepMerge_PartialBatch_ReplacesLinearityOnly()
    {
        var product = new TdsSensorSpecs
        {
            Charts = new TdsChartData
            {
                Linearity = [new() { X = 0, Y = 0 }, new() { X = 100, Y = 99 }],
            },
        };

        var batch = new TdsSensorSpecs
        {
            Charts = new TdsChartData
            {
                Linearity = [new() { X = 0, Y = 0 }, new() { X = 100, Y = 100 }],
            },
        };

        var merged = TdsSensorDataMerger.DeepMerge(product, batch);

        Assert.Equal(100, merged.Charts.Linearity[1].Y);
    }

    [Fact]
    public void MergeService_UsesLatestBatchPartialSpecs()
    {
        var product = new Product { TdsSensorSpecsJson = """{"rangeMax":500,"hysteresisPercent":2}""" };
        var batch = new TestBatch
        {
            TestValues = """{"hysteresisPercent":1.1}""",
            BatchNumber = "LT-1",
        };

        var service = new TdsDataMergeService();
        var merged = service.Merge(product, batch);

        Assert.Equal(1.1, merged.HysteresisPercent);
        Assert.Equal(500, merged.RangeMax);
        Assert.Equal("LT-1", service.ResolveBatchNumber(batch));
    }

    [Fact]
    public void ParseProductSpecs_LegacyJson_UpgradesToSchemaV2()
    {
        var specs = TdsSensorDataMerger.ParseProductSpecs("""{"forceUnit":"N","rangeMax":100}""");

        Assert.Equal(2, specs.SchemaVersion);
        Assert.Equal(100, specs.RangeMax);
    }

    [Fact]
    public void DeepMerge_MergesPlaceholderAndAssetDictionaries()
    {
        var product = new TdsSensorSpecs
        {
            PlaceholderValues = new Dictionary<string, string> { ["CustomText"] = "A" },
            TemplateAssets = new Dictionary<string, string> { ["Logo"] = "logo-a.png" },
        };

        var batch = new TdsSensorSpecs
        {
            PlaceholderValues = new Dictionary<string, string> { ["CustomText"] = "B" },
            TemplateAssets = new Dictionary<string, string> { ["ProductImage"] = "product.png" },
        };

        var merged = TdsSensorDataMerger.DeepMerge(product, batch);

        Assert.Equal("B", merged.PlaceholderValues["CustomText"]);
        Assert.Equal("logo-a.png", merged.TemplateAssets["Logo"]);
        Assert.Equal("product.png", merged.TemplateAssets["ProductImage"]);
    }
}
