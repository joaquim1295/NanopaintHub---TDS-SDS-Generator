using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

class Program
{
    static void Main()
    {
        string transcriptPath = @"C:\Users\Arriscado\.gemini\antigravity-ide\brain\f1e4228c-e6ce-4b95-b5e1-f283cde4eb94\.system_generated\logs\transcript.jsonl";
        var lines = File.ReadAllLines(transcriptPath);
        string lastContent = null;
        
        foreach (var line in lines)
        {
            if (line.Contains("\"TargetFile\":\"C:\\\\Users\\\\Arriscado\\\\Documents\\\\Nanopaint\\\\NanopaintHub\\\\Clients\\\\NanopaintHub.WinUiClient\\\\Views\\\\TemplateEditorPage.xaml\""))
            {
                var match = Regex.Match(line, @"""CodeContent"":""(.*?)""\,", RegexOptions.Singleline);
                if (match.Success)
                {
                    string content = match.Groups[1].Value.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t").Replace("\\\\", "\\");
                    if (content.Split("\n").Length > 100)
                    {
                        lastContent = content;
                    }
                }
            }
        }
        
        if (lastContent != null)
        {
            File.WriteAllText(@"C:\Users\Arriscado\Documents\Nanopaint\NanopaintHub\Recovered.xaml", lastContent);
            Console.WriteLine("Recovered XAML saved from write_to_file.");
        }
        else
        {
            Console.WriteLine("Not found XAML with >100 lines.");
        }
    }
}
