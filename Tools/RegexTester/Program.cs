using System;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string text = @"SECTION 3: Composition/information on ingredients
3.1. Substances
Identification of the substance:
Trade code: G0806056
Qty Name Ident. Number Classification
>= 90% Butildiglicole Index number: 603-096-00-8 3.3/2 Eye Irrit. 2 H319
CAS: 112-34-5
EC: 203-961-6";

        var CasRegex = new Regex(@"\b[1-9]\d{1,6}-\d{2}-\d\b", RegexOptions.Compiled);
        var match = CasRegex.Match(text);
        if (match.Success)
        {
            Console.WriteLine("CAS matched: " + match.Value);
            int start = Math.Max(0, match.Index - 150);
            int length = match.Index - start;
            string beforeCas = text.Substring(start, length);
            Console.WriteLine("Before CAS:");
            Console.WriteLine(beforeCas);
            
            var pctRegex = new Regex(@"(?:<|>|>=|<=)?\s*\d+(?:\.\d+)?\s*(?:-\s*\d+(?:\.\d+)?)?\s*%", RegexOptions.RightToLeft);
            var pctMatch = pctRegex.Match(beforeCas);
            if (pctMatch.Success)
            {
                Console.WriteLine("Found pct: " + pctMatch.Value);
            }
            else
            {
                Console.WriteLine("PCT MATCH FAILED!");
            }
        }
    }
}
