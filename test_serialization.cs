using System;
using System.Text.Json;
using NanopaintHub_WinUIClient.Models;

public class Program 
{
    public static void Main()
    {
        var model = new TemplateBuilderModel();
        model.Header.HeaderRepeatStyle = "Minimal";
        model.Footer.FooterRepeatStyle = "Original";
        
        string json = JsonSerializer.Serialize(model);
        Console.WriteLine("JSON: " + json);
        
        var deserialized = JsonSerializer.Deserialize<TemplateBuilderModel>(json);
        Console.WriteLine("Deserialized Header: " + deserialized.Header.HeaderRepeatStyle);
        Console.WriteLine("Deserialized Footer: " + deserialized.Footer.FooterRepeatStyle);
    }
}
