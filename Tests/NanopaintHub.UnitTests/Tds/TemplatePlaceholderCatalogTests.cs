using NanopaintHub.Application.Tds;

namespace NanopaintHub.UnitTests.Tds;

public sealed class TemplatePlaceholderCatalogTests
{
    [Fact]
    public void ForDocumentType_Tds_ReturnsCoreAndTdsEntries()
    {
        var items = TemplatePlaceholderCatalog.ForDocumentType("TDS");

        Assert.Contains(items, x => x.Token == "Name");
        Assert.Contains(items, x => x.Token == "Sensor.SensitivityVolts");
        Assert.DoesNotContain(items, x => x.Token == "Components");
    }

    [Fact]
    public void ForDocumentType_Sds_ReturnsComponentsEntry()
    {
        var items = TemplatePlaceholderCatalog.ForDocumentType("SDS");

        Assert.Contains(items, x => x.Token == "Name");
        Assert.Contains(items, x => x.Token == "Components");
    }
}
