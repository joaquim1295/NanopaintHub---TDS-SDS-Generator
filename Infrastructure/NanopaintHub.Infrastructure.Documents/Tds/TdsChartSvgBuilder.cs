using System.Globalization;
using System.Text;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Tds;

namespace NanopaintHub.Infrastructure.Documents.Tds;

public sealed class TdsChartSvgBuilder : ITdsChartRenderer
{
    private const int Width = 480;
    private const int Height = 260;
    private const int PadLeft = 48;
    private const int PadRight = 16;
    private const int PadTop = 20;
    private const int PadBottom = 36;

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
                "Saída (%)");
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
                "Resistência (Ω)");
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

    private static string RenderLineChart(
        IReadOnlyList<TdsChartPoint> points,
        string title,
        string xLabel,
        string yLabel)
    {
        var xs = points.Select(p => p.X).ToList();
        var ys = points.Select(p => p.Y).ToList();
        var minX = xs.Min();
        var maxX = xs.Max();
        var minY = ys.Min();
        var maxY = ys.Max();
        if (Math.Abs(maxX - minX) < 1e-9)
        {
            maxX = minX + 1;
        }

        if (Math.Abs(maxY - minY) < 1e-9)
        {
            maxY = minY + 1;
        }

        var plotW = Width - PadLeft - PadRight;
        var plotH = Height - PadTop - PadBottom;

        double MapX(double x) => PadLeft + (x - minX) / (maxX - minX) * plotW;
        double MapY(double y) => PadTop + plotH - (y - minY) / (maxY - minY) * plotH;

        var poly = string.Join(
            " ",
            points.Select(p => $"{MapX(p.X).ToString("F1", CultureInfo.InvariantCulture)},{MapY(p.Y).ToString("F1", CultureInfo.InvariantCulture)}"));

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\" width=\"100%\" role=\"img\">");
        sb.Append($"<title>{Escape(title)}</title>");
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fafafa\" stroke=\"#ddd\"/>");
        sb.Append($"<text x=\"{Width / 2}\" y=\"14\" text-anchor=\"middle\" font-size=\"12\" font-family=\"Segoe UI,Arial\">{Escape(title)}</text>");
        sb.Append($"<polyline fill=\"none\" stroke=\"#0078d4\" stroke-width=\"2\" points=\"{poly}\"/>");
        sb.Append($"<text x=\"{Width / 2}\" y=\"{Height - 6}\" text-anchor=\"middle\" font-size=\"10\" font-family=\"Segoe UI,Arial\">{Escape(xLabel)}</text>");
        sb.Append($"<text x=\"12\" y=\"{PadTop + plotH / 2}\" transform=\"rotate(-90 12 {PadTop + plotH / 2})\" text-anchor=\"middle\" font-size=\"10\" font-family=\"Segoe UI,Arial\">{Escape(yLabel)}</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string RenderHysteresisChart(TdsHysteresisChart chart, string forceUnit)
    {
        var all = chart.Loading.Concat(chart.Unloading).ToList();
        var minX = all.Min(p => p.X);
        var maxX = all.Max(p => p.X);
        var minY = all.Min(p => p.Y);
        var maxY = all.Max(p => p.Y);
        if (Math.Abs(maxX - minX) < 1e-9)
        {
            maxX = minX + 1;
        }

        if (Math.Abs(maxY - minY) < 1e-9)
        {
            maxY = minY + 1;
        }

        var plotW = Width - PadLeft - PadRight;
        var plotH = Height - PadTop - PadBottom;

        double MapX(double x) => PadLeft + (x - minX) / (maxX - minX) * plotW;
        double MapY(double y) => PadTop + plotH - (y - minY) / (maxY - minY) * plotH;

        string Poly(IReadOnlyList<TdsChartPoint> pts) => string.Join(
            " ",
            pts.Select(p => $"{MapX(p.X).ToString("F1", CultureInfo.InvariantCulture)},{MapY(p.Y).ToString("F1", CultureInfo.InvariantCulture)}"));

        var pct = chart.Percent.HasValue ? $" ({chart.Percent.Value:G3}%)" : string.Empty;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\" width=\"100%\" role=\"img\">");
        sb.Append($"<title>Histerese{pct}</title>");
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fafafa\" stroke=\"#ddd\"/>");
        sb.Append($"<text x=\"{Width / 2}\" y=\"14\" text-anchor=\"middle\" font-size=\"12\" font-family=\"Segoe UI,Arial\">Histerese{pct}</text>");
        sb.Append($"<polyline fill=\"none\" stroke=\"#107c10\" stroke-width=\"2\" points=\"{Poly(chart.Loading)}\"/>");
        sb.Append($"<polyline fill=\"none\" stroke=\"#d13438\" stroke-width=\"2\" stroke-dasharray=\"4 3\" points=\"{Poly(chart.Unloading)}\"/>");
        sb.Append($"<text x=\"{Width / 2}\" y=\"{Height - 6}\" text-anchor=\"middle\" font-size=\"10\" font-family=\"Segoe UI,Arial\">Força ({forceUnit})</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
