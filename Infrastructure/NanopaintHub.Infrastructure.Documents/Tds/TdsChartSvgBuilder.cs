using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;

namespace NanopaintHub.Infrastructure.Documents.Tds;

public sealed class TdsChartSvgBuilder : ITdsChartRenderer
{
    private const int Width = 480;
    private const int Height = 260;
    private const int PadLeft = 55;
    private const int PadRight = 20;
    private const int PadTop = 30;
    private const int PadBottom = 45;

    public IReadOnlyDictionary<string, string> RenderAll(
        TdsSensorSpecs specs,
        string? linearityImagePath = null,
        string? resistanceImagePath = null,
        string? hysteresisImagePath = null)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (TryEmbedImage(linearityImagePath, "Linearity", out var linearityImg))
        {
            result["linearity"] = linearityImg;
        }
        else if (specs.Charts.Linearity.Count >= 2)
        {
            result["linearity"] = RenderLineChart(
                specs.Charts.Linearity,
                $"Linearidade ({specs.ForceUnit})",
                $"Força ({specs.ForceUnit})",
                "Saída (%)",
                "#2563eb");
        }

        if (TryEmbedImage(resistanceImagePath, "Resistance vs Ohms", out var resistanceImg))
        {
            result["resistance"] = resistanceImg;
        }
        else if (specs.Charts.ResistanceVsForce.Count >= 2)
        {
            result["resistance"] = RenderLineChart(
                specs.Charts.ResistanceVsForce,
                "Resistance vs Force",
                $"Força ({specs.ForceUnit})",
                "Resistência (Ω)",
                "#2563eb");
        }

        var hyst = specs.Charts.Hysteresis;
        if (TryEmbedImage(hysteresisImagePath, "Hysteresis", out var hystImg))
        {
            result["hysteresis"] = hystImg;
        }
        else if (hyst is not null && hyst.Loading.Count >= 2 && hyst.Unloading.Count >= 2)
        {
            result["hysteresis"] = RenderHysteresisChart(hyst, specs.ForceUnit);
        }

        return result;
    }

    private static bool TryEmbedImage(string? path, string title, out string markup)
    {
        markup = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => null,
        };

        if (mime is null)
        {
            return false;
        }

        var bytes = File.ReadAllBytes(path);
        var b64 = Convert.ToBase64String(bytes);
        markup =
            $"<figure><figcaption>{Escape(title)}</figcaption>" +
            $"<img alt=\"{Escape(title)}\" style=\"max-width:100%;height:auto\" src=\"data:{mime};base64,{b64}\" /></figure>";
        return true;
    }

    private static (double Min, double Max, double TickSpacing) CalculateNiceScale(double min, double max)
    {
        double range = max - min;
        if (range == 0) range = 1;

        double exactTickSpacing = range / 5.0; // Target ~5 ticks
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(exactTickSpacing)));
        double fraction = exactTickSpacing / magnitude;

        double niceTickSpacing = magnitude;
        if (fraction < 1.5) niceTickSpacing = magnitude;
        else if (fraction < 3) niceTickSpacing = magnitude * 2;
        else if (fraction < 7) niceTickSpacing = magnitude * 5;
        else niceTickSpacing = magnitude * 10;

        double niceMin = Math.Floor(min / niceTickSpacing) * niceTickSpacing;
        double niceMax = Math.Ceiling(max / niceTickSpacing) * niceTickSpacing;

        if (Math.Abs(niceMax - niceMin) < 1e-9) niceMax = niceMin + niceTickSpacing;

        return (niceMin, niceMax, niceTickSpacing);
    }

    private static void AppendGridAndAxes(
        StringBuilder sb,
        double minX, double maxX, double tickX,
        double minY, double maxY, double tickY,
        int plotW, int plotH,
        Func<double, double> mapX, Func<double, double> mapY)
    {
        // Draw grid lines
        for (double y = minY; y <= maxY + 1e-9; y += tickY)
        {
            var mappedY = mapY(y);
            var my = mappedY.ToString("F1", CultureInfo.InvariantCulture);
            sb.Append($"<line x1=\"{PadLeft}\" y1=\"{my}\" x2=\"{PadLeft + plotW}\" y2=\"{my}\" stroke=\"#cbd5e1\" stroke-dasharray=\"2 3\" stroke-width=\"0.8\" />");
            sb.Append($"<text x=\"{PadLeft - 8}\" y=\"{mappedY + 3:F1}\" text-anchor=\"end\" font-size=\"10\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#475569\">{Math.Round(y, 2).ToString(CultureInfo.InvariantCulture)}</text>");
        }

        for (double x = minX; x <= maxX + 1e-9; x += tickX)
        {
            var mappedX = mapX(x);
            var mx = mappedX.ToString("F1", CultureInfo.InvariantCulture);
            sb.Append($"<line x1=\"{mx}\" y1=\"{PadTop}\" x2=\"{mx}\" y2=\"{PadTop + plotH}\" stroke=\"#cbd5e1\" stroke-dasharray=\"2 3\" stroke-width=\"0.8\" />");
            sb.Append($"<text x=\"{mx}\" y=\"{PadTop + plotH + 16}\" text-anchor=\"middle\" font-size=\"10\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#475569\">{Math.Round(x, 2).ToString(CultureInfo.InvariantCulture)}</text>");
        }

        // Draw axis lines
        sb.Append($"<polyline fill=\"none\" stroke=\"#94a3b8\" stroke-width=\"1.5\" points=\"{PadLeft},{PadTop} {PadLeft},{PadTop + plotH} {PadLeft + plotW},{PadTop + plotH}\"/>");
    }

    private static void AppendDataPoints(StringBuilder sb, IReadOnlyList<TdsChartPoint> points, Func<double, double> mapX, Func<double, double> mapY, string color)
    {
        foreach (var p in points)
        {
            var cx = mapX(p.X).ToString("F1", CultureInfo.InvariantCulture);
            var cy = mapY(p.Y).ToString("F1", CultureInfo.InvariantCulture);
            sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"3\" fill=\"#ffffff\" stroke=\"{color}\" stroke-width=\"1.5\" />");
        }
    }

    private static string RenderLineChart(
        IReadOnlyList<TdsChartPoint> points,
        string title,
        string xLabel,
        string yLabel,
        string color)
    {
        var xs = points.Select(p => p.X).ToList();
        var ys = points.Select(p => p.Y).ToList();
        var xStats = CalculateNiceScale(xs.Min(), xs.Max());
        var yStats = CalculateNiceScale(ys.Min(), ys.Max());

        var plotW = Width - PadLeft - PadRight;
        var plotH = Height - PadTop - PadBottom;

        double MapX(double x) => PadLeft + (x - xStats.Min) / (xStats.Max - xStats.Min) * plotW;
        double MapY(double y) => PadTop + plotH - (y - yStats.Min) / (yStats.Max - yStats.Min) * plotH;

        var poly = string.Join(
            " ",
            points.Select(p => $"{MapX(p.X).ToString("F1", CultureInfo.InvariantCulture)},{MapY(p.Y).ToString("F1", CultureInfo.InvariantCulture)}"));

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\" width=\"100%\" role=\"img\">");
        sb.Append($"<title>{Escape(title)}</title>");
        
        // Background
        sb.Append($"<rect x=\"{PadLeft}\" y=\"{PadTop}\" width=\"{plotW}\" height=\"{plotH}\" fill=\"#f8fafc\" />");
        
        // Grid and Axes
        AppendGridAndAxes(sb, xStats.Min, xStats.Max, xStats.TickSpacing, yStats.Min, yStats.Max, yStats.TickSpacing, plotW, plotH, MapX, MapY);

        // Titles
        sb.Append($"<text x=\"{Width / 2}\" y=\"16\" text-anchor=\"middle\" font-size=\"12\" font-weight=\"600\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#1e293b\">{Escape(title)}</text>");
        sb.Append($"<text x=\"{Width / 2}\" y=\"{Height - 10}\" text-anchor=\"middle\" font-size=\"11\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#475569\">{Escape(xLabel)}</text>");
        sb.Append($"<text x=\"16\" y=\"{PadTop + plotH / 2}\" transform=\"rotate(-90 16 {PadTop + plotH / 2})\" text-anchor=\"middle\" font-size=\"11\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#475569\">{Escape(yLabel)}</text>");
        
        // Data Line
        sb.Append($"<polyline fill=\"none\" stroke=\"{color}\" stroke-width=\"2\" points=\"{poly}\"/>");
        
        // Data Points
        AppendDataPoints(sb, points, MapX, MapY, color);

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string RenderHysteresisChart(TdsHysteresisChart chart, string forceUnit)
    {
        var all = chart.Loading.Concat(chart.Unloading).ToList();
        var xStats = CalculateNiceScale(all.Min(p => p.X), all.Max(p => p.X));
        var yStats = CalculateNiceScale(all.Min(p => p.Y), all.Max(p => p.Y));

        var plotW = Width - PadLeft - PadRight;
        var plotH = Height - PadTop - PadBottom;

        double MapX(double x) => PadLeft + (x - xStats.Min) / (xStats.Max - xStats.Min) * plotW;
        double MapY(double y) => PadTop + plotH - (y - yStats.Min) / (yStats.Max - yStats.Min) * plotH;

        string Poly(IReadOnlyList<TdsChartPoint> pts) => string.Join(
            " ",
            pts.Select(p => $"{MapX(p.X).ToString("F1", CultureInfo.InvariantCulture)},{MapY(p.Y).ToString("F1", CultureInfo.InvariantCulture)}"));

        var pct = chart.Percent.HasValue ? $" ({chart.Percent.Value:G3}%)" : string.Empty;
        var title = $"Histerese{pct}";

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\" width=\"100%\" role=\"img\">");
        sb.Append($"<title>{Escape(title)}</title>");

        // Background
        sb.Append($"<rect x=\"{PadLeft}\" y=\"{PadTop}\" width=\"{plotW}\" height=\"{plotH}\" fill=\"#f8fafc\" />");
        
        // Grid and Axes
        AppendGridAndAxes(sb, xStats.Min, xStats.Max, xStats.TickSpacing, yStats.Min, yStats.Max, yStats.TickSpacing, plotW, plotH, MapX, MapY);

        // Titles
        sb.Append($"<text x=\"{Width / 2}\" y=\"16\" text-anchor=\"middle\" font-size=\"12\" font-weight=\"600\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#1e293b\">{Escape(title)}</text>");
        sb.Append($"<text x=\"{Width / 2}\" y=\"{Height - 10}\" text-anchor=\"middle\" font-size=\"11\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#475569\">Força ({Escape(forceUnit)})</text>");
        sb.Append($"<text x=\"16\" y=\"{PadTop + plotH / 2}\" transform=\"rotate(-90 16 {PadTop + plotH / 2})\" text-anchor=\"middle\" font-size=\"11\" font-family=\"Segoe UI, Arial, sans-serif\" fill=\"#475569\">Saída (%)</text>");
        
        // Loading Line (Green)
        sb.Append($"<polyline fill=\"none\" stroke=\"#16a34a\" stroke-width=\"2\" points=\"{Poly(chart.Loading)}\"/>");
        // Unloading Line (Red Dashed)
        sb.Append($"<polyline fill=\"none\" stroke=\"#dc2626\" stroke-width=\"2\" stroke-dasharray=\"4 3\" points=\"{Poly(chart.Unloading)}\"/>");

        // Data Points
        AppendDataPoints(sb, chart.Loading, MapX, MapY, "#16a34a");
        AppendDataPoints(sb, chart.Unloading, MapX, MapY, "#dc2626");

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
