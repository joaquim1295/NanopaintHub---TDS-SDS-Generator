using NanopaintHub.Application.Tds;

namespace NanopaintHub.Application.Contracts;

public interface ITdsChartRenderer
{
    IReadOnlyDictionary<string, string> RenderAll(
        TdsSensorSpecs specs,
        string? linearityImagePath = null,
        string? resistanceImagePath = null,
        string? hysteresisImagePath = null);
}
