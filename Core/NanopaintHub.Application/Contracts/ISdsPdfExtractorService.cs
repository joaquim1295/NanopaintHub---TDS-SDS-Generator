namespace NanopaintHub.Application.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISdsPdfExtractorService
{
    /// <summary>
    /// Reads a supplier's PDF and extracts a list of found CAS numbers along with their potential percentages (if readable).
    /// </summary>
    Task<SdsExtractionResult> ExtractSdsDataAsync(string pdfFilePath);
}

public class SdsExtractionResult
{
    public string ProductName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public List<ExtractedCasData> CasNumbers { get; set; } = new();
}

public class ExtractedCasData
{
    public string CasNumber { get; set; } = string.Empty;
    public string RawPercentageText { get; set; } = string.Empty;
    public double ParsedPercentage { get; set; }
    public List<string> ExtractedHazards { get; set; } = new();
}
