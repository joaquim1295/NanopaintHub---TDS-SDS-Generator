namespace NanopaintHub.Infrastructure.Documents.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using NanopaintHub.Application.Contracts;

public class SdsPdfExtractorService : ISdsPdfExtractorService
{
    // Regex for CAS Number format (e.g. 112-34-5)
    // Most CAS numbers are up to 7 digits, hyphen, 2 digits, hyphen, 1 digit.
    private static readonly Regex CasRegex = new Regex(@"\b[1-9]\d{1,6}-\d{2}-\d\b", RegexOptions.Compiled);

    public async Task<SdsExtractionResult> ExtractSdsDataAsync(string pdfFilePath)
    {
        return await Task.Run(() =>
        {
            var result = new SdsExtractionResult();
            var foundCas = new Dictionary<string, ExtractedCasData>();

            using var pdfReader = new PdfReader(pdfFilePath);
            using var pdfDocument = new PdfDocument(pdfReader);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);
                
                // Removed fragile heuristics for Product Name and Supplier
                // The AI team determined it's better to let the user fill these manually or use the typed search query
                // because PDF formatting is too unpredictable without an LLM OCR.

                var matches = CasRegex.Matches(text);
                foreach (Match match in matches)
                {
                    string cas = match.Value;
                    if (!foundCas.ContainsKey(cas))
                    {
                        foundCas[cas] = new ExtractedCasData { CasNumber = cas };
                    }
                    
                    var casData = foundCas[cas];
                    
                    // Look around the CAS match (150 chars before and 150 chars after)
                    int start = Math.Max(0, match.Index - 150);
                    int length = Math.Min(text.Length - start, 300);
                    string aroundCas = text.Substring(start, length);
                    
                    // Regex to find percentages like ">= 90%", "< 5 %", "10-20%"
                    var pctRegex = new Regex(@"(?:<|>|>=|<=)?\s*\d+(?:\.\d+)?\s*(?:-\s*\d+(?:\.\d+)?)?\s*%", RegexOptions.RightToLeft);
                    var pctMatch = pctRegex.Match(aroundCas);
                    
                    if (pctMatch.Success)
                    {
                        // Extract numbers and take the max (upper bound of a range)
                        var numRegex = new Regex(@"\d+(?:\.\d+)?");
                        var numMatches = numRegex.Matches(pctMatch.Value);
                        double maxVal = 0;
                        foreach(Match num in numMatches)
                        {
                            if (double.TryParse(num.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                            {
                                if (v > maxVal) maxVal = v;
                            }
                        }
                        
                        // Update if we found a higher percentage (in case first occurrence was 0)
                        if (maxVal > casData.ParsedPercentage)
                        {
                            casData.RawPercentageText = pctMatch.Value.Trim();
                            casData.ParsedPercentage = maxVal;
                        }
                    }

                    // Extract Hazards (e.g. H319, H225)
                    var hazardRegex = new Regex(@"\bH\d{3}[a-zA-Z]*\b");
                    var hazardMatches = hazardRegex.Matches(aroundCas);
                    foreach (Match hMatch in hazardMatches)
                    {
                        var hazardCode = hMatch.Value;
                        if (!casData.ExtractedHazards.Contains(hazardCode))
                        {
                            casData.ExtractedHazards.Add(hazardCode);
                        }
                    }
                }
            }

            result.CasNumbers = foundCas.Values.ToList();
            return result;
        });
    }
}
