using System;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string text = ">= 90% Butildiglicole Index number: 603-096-00-8 3.3/2 Eye Irrit. 2 H319\nCAS: 112-34-5\nEC: 203-961-6";
        var CasRegex = new Regex(@"\b[1-9]\d{1,6}-\d{2}-\d\b");
        var matches = CasRegex.Matches(text);
        
        foreach (Match match in matches) {
            string cas = match.Value;
            Console.WriteLine($"Found CAS: {cas}");
            
            int start = Math.Max(0, match.Index - 150);
            int length = Math.Min(text.Length - start, 300);
            string aroundCas = text.Substring(start, length);
            
            var hazardRegex = new Regex(@"\bH\d{3}[a-zA-Z]*\b");
            var hazardMatches = hazardRegex.Matches(aroundCas);
            foreach (Match hMatch in hazardMatches) {
                Console.WriteLine($"Found Hazard: {hMatch.Value}");
            }
        }
    }
}
