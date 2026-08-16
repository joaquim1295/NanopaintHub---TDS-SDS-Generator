using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NanopaintHub.Application.Tds;

/// <summary>
/// Parser resiliente em C# para conversão de ficheiros de dados (.csv, .tsv, .txt) 
/// em pontos de gráfico (TdsChartPoint) e curvas de Histerese para a Nanopaint.
/// </summary>
public static class CsvChartDataParser
{
    private static readonly char[] Delimiters = [';', '\t', ',', '|'];

    /// <summary>
    /// Lê um ficheiro de texto (CSV/TSV) e extrai uma lista de pontos (X, Y).
    /// </summary>
    public static List<TdsChartPoint> ParseXYPointsFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return [];
        }

        var content = File.ReadAllText(filePath);
        return ParseXYPoints(content);
    }

    /// <summary>
    /// Converte uma string formatada em CSV/TSV numa lista de pontos (X, Y).
    /// </summary>
    public static List<TdsChartPoint> ParseXYPoints(string content)
    {
        var points = new List<TdsChartPoint>();
        if (string.IsNullOrWhiteSpace(content)) return points;

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
            {
                continue;
            }

            var parts = SplitLine(line);
            if (parts.Length < 2) continue;

            if (TryParseDouble(parts[0], out double x) && TryParseDouble(parts[1], out double y))
            {
                points.Add(new TdsChartPoint { X = x, Y = y });
            }
        }

        return points;
    }

    /// <summary>
    /// Lê um ficheiro CSV/TSV e extrai dados de histerese (X, Loading, Unloading).
    /// </summary>
    public static TdsHysteresisChart ParseHysteresisChartFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new TdsHysteresisChart();
        }

        var content = File.ReadAllText(filePath);
        return ParseHysteresisChart(content);
    }

    /// <summary>
    /// Converte uma string formatada em CSV/TSV em curvas de carga e descarga para Histerese.
    /// </summary>
    public static TdsHysteresisChart ParseHysteresisChart(string content)
    {
        var chart = new TdsHysteresisChart();
        if (string.IsNullOrWhiteSpace(content)) return chart;

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
            {
                continue;
            }

            var parts = SplitLine(line);
            if (parts.Length < 2) continue;

            if (parts.Length >= 3)
            {
                // Formato 3 colunas: X, Loading, Unloading
                if (TryParseDouble(parts[0], out double x) &&
                    TryParseDouble(parts[1], out double loadY) &&
                    TryParseDouble(parts[2], out double unloadY))
                {
                    chart.Loading.Add(new TdsChartPoint { X = x, Y = loadY });
                    chart.Unloading.Add(new TdsChartPoint { X = x, Y = unloadY });
                }
            }
            else
            {
                // Formato 2 colunas padrão (Carga)
                if (TryParseDouble(parts[0], out double x) && TryParseDouble(parts[1], out double y))
                {
                    chart.Loading.Add(new TdsChartPoint { X = x, Y = y });
                }
            }
        }

        // Cálculo automático de % de Histerese se houver dados em ambas as séries
        if (chart.Loading.Count > 0 && chart.Unloading.Count > 0)
        {
            double maxDelta = 0;
            double minY = chart.Loading.Min(p => p.Y);
            double maxY = chart.Loading.Max(p => p.Y);
            double span = maxY - minY;

            int count = Math.Min(chart.Loading.Count, chart.Unloading.Count);
            for (int i = 0; i < count; i++)
            {
                double delta = Math.Abs(chart.Loading[i].Y - chart.Unloading[i].Y);
                if (delta > maxDelta) maxDelta = delta;
            }

            if (span > 0)
            {
                chart.Percent = Math.Round((maxDelta / span) * 100.0, 2);
            }
        }

        return chart;
    }

    private static string[] SplitLine(string line)
    {
        char selectedDelimiter = ',';
        foreach (var delim in Delimiters)
        {
            if (line.Contains(delim))
            {
                selectedDelimiter = delim;
                break;
            }
        }

        return line.Split(selectedDelimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool TryParseDouble(string input, out double value)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            value = 0;
            return false;
        }

        // Sanitização de unidades comuns em CSVs de laboratório (ex: "10 N", "100.5 %", "12.5k")
        var sanitized = input.Replace("N", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("%", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("V", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("Ω", "", StringComparison.OrdinalIgnoreCase)
                             .Replace("Ohm", "", StringComparison.OrdinalIgnoreCase)
                             .Trim();

        // Se contiver vírgula decimal (ex: "10,5"), substituir por ponto para o InvariantCulture
        if (sanitized.Contains(',') && !sanitized.Contains('.'))
        {
            sanitized = sanitized.Replace(',', '.');
        }

        return double.TryParse(sanitized, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }
}
