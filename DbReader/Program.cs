using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string transcriptPath = @"C:\Users\Arriscado\.gemini\antigravity-ide\brain\f1e4228c-e6ce-4b95-b5e1-f283cde4eb94\.system_generated\logs\transcript.jsonl";
        var lines = File.ReadAllLines(transcriptPath);
        var output = new List<string>();
        
        foreach (var line in lines)
        {
            if (line.Contains("TemplateEditorPage.xaml"))
            {
                if (line.Contains("ReplacementChunks"))
                {
                    var match = Regex.Match(line, @"""ReplacementContent"":""(.*?)""");
                    if (match.Success)
                    {
                        output.Add("--- REPLACEMENT CHUNK ---");
                        output.Add(match.Groups[1].Value.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t").Replace("\\\\", "\\"));
                    }
                }
                else if (line.Contains("CodeContent"))
                {
                    var match = Regex.Match(line, @"""CodeContent"":""(.*?)""");
                    if (match.Success)
                    {
                        output.Add("--- FULL FILE OVERWRITE ---");
                        output.Add(match.Groups[1].Value.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t").Replace("\\\\", "\\"));
                    }
                }
                else if (line.Contains("The following code has been modified"))
                {
                    var match = Regex.Match(line, @"Showing lines 1 to \d+\\nThe following code has been modified.*?: <original_line>\..*?\\n(\d+: .*)\\nThe above content shows the entire", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        output.Add("--- VIEW FILE ---");
                        output.Add(Regex.Replace(match.Groups[1].Value, @"(?m)^\d+: ", "").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\"));
                    }
                }
            }
        }
        
        File.WriteAllLines(@"C:\Users\Arriscado\Documents\Nanopaint\NanopaintHub\AllXamlHistory.txt", output);
        Console.WriteLine("Done.");
    }
}
