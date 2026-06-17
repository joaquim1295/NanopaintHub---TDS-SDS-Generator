using System.Text;
using NanopaintHub_WinUIClient.Models;

namespace NanopaintHub_WinUIClient.Services;

public static class TdsHtmlGenerator
{
    public static string GenerateHtml(TemplateBuilderModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"  <div class=\"page\">");

        // Header
        string headerStyle = "";
        if (!string.IsNullOrWhiteSpace(model.Header.FontFamily) || !string.IsNullOrWhiteSpace(model.Header.TextColor))
        {
            var f = !string.IsNullOrWhiteSpace(model.Header.FontFamily) ? $"font-family: '{model.Header.FontFamily}', sans-serif;" : "";
            var c = !string.IsNullOrWhiteSpace(model.Header.TextColor) ? $"color: {model.Header.TextColor};" : "";
            headerStyle = $" style=\"{f} {c}\"";
        }
        sb.AppendLine($"    <header class=\"header-banner\"{headerStyle}>");
        sb.AppendLine("      <div class=\"header-top-row\">");
        sb.AppendLine("        <div class=\"logo-wrap\">");
        if (model.Header.ShowLogo)
        {
            var logoUrl = string.IsNullOrWhiteSpace(model.Header.LogoUrl) ? "{{Assets.Logo}}" : model.Header.LogoUrl;
            sb.AppendLine($"          <img class=\"logo-image\" src=\"{logoUrl}\" alt=\"Logo\" height=\"{model.Header.LogoHeight}\" />");
        }
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"doc-meta\">");
        var infoCat = string.IsNullOrWhiteSpace(model.Header.InfoCategory) ? "Functional Inks" : model.Header.InfoCategory;
        var infoType = string.IsNullOrWhiteSpace(model.Header.InfoDocType) ? "Technical Data Sheet" : model.Header.InfoDocType;
        var infoRev = model.Header.InfoRevisedDate.HasValue ? model.Header.InfoRevisedDate.Value.ToString("yyyy-MM-dd") : "{{RevisionDate}}";
        var infoVer = string.IsNullOrWhiteSpace(model.Header.InfoVersion) ? "Version {{Version}}" : model.Header.InfoVersion;
        
        sb.AppendLine($"          <strong>{infoCat}</strong>");
        sb.AppendLine($"          {infoType}<br />");
        sb.AppendLine($"          Revised: {infoRev} · {infoVer}");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        
        var idType = string.IsNullOrWhiteSpace(model.Header.IdentType) ? "Piezoresistive Ink" : model.Header.IdentType;
        var idName = string.IsNullOrWhiteSpace(model.Header.IdentName) ? "{{Name}} · {{Code}}" : model.Header.IdentName;
        var idDesc = string.IsNullOrWhiteSpace(model.Header.IdentDesc) ? "{{Description}}" : model.Header.IdentDesc;

        sb.AppendLine($"      <h1 class=\"product-title\">{idType}</h1>");
        sb.AppendLine($"      <p class=\"product-subtitle\">{idName}</p>");
        sb.AppendLine($"      <p class=\"product-description\">{idDesc}</p>");
        sb.AppendLine("    </header>");

        // Main Content
        sb.AppendLine("    <main class=\"main-content\">");
        sb.AppendLine("      {{#if HasBatch}}");
        sb.AppendLine("      <p class=\"batch-note\">Batch data applied: <strong>{{BatchNumber}}</strong> (merged with product specifications where applicable).</p>");
        sb.AppendLine("      {{/if}}");

        bool insideTwoColumn = false;

        foreach (var sec in model.Sections)
        {
            if (sec.PageBreakBefore)
            {
                if (insideTwoColumn)
                {
                    sb.AppendLine("      </div>");
                    insideTwoColumn = false;
                }
                sb.AppendLine("      <div class=\"page-break-preview\"><span>--- QUEBRA DE PÁGINA ---</span></div>");
                sb.AppendLine("      <div style=\"page-break-before: always; break-before: page;\"></div>");
            }

            if (sec.IsHalfWidth)
            {
                if (!insideTwoColumn)
                {
                    sb.AppendLine("      <div class=\"two-column\">");
                    insideTwoColumn = true;
                }
            }
            else
            {
                if (insideTwoColumn)
                {
                    sb.AppendLine("      </div>");
                    insideTwoColumn = false;
                }
            }

            GenerateSectionHtml(sb, sec);
        }

        if (insideTwoColumn)
        {
            sb.AppendLine("      </div>");
        }

        sb.AppendLine("    </main>");

        // Footer
        string footerStyle = "";
        if (!string.IsNullOrWhiteSpace(model.Footer.FontFamily) || !string.IsNullOrWhiteSpace(model.Footer.TextColor))
        {
            var f = !string.IsNullOrWhiteSpace(model.Footer.FontFamily) ? $"font-family: '{model.Footer.FontFamily}', sans-serif;" : "";
            var c = !string.IsNullOrWhiteSpace(model.Footer.TextColor) ? $"color: {model.Footer.TextColor};" : "";
            footerStyle = $" style=\"{f} {c}\"";
        }
        sb.AppendLine($"    <footer class=\"footer-area\"{footerStyle}>");
        sb.AppendLine("      <div class=\"footer-cols\">");
        sb.AppendLine($"        <div class=\"footer-col1\">{model.Footer.Column1.Replace("\n", "<br/>")}</div>");
        sb.AppendLine($"        <div class=\"footer-col2\">{model.Footer.Column2.Replace("\n", "<br/>")}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <p class=\"footer-legal\">{model.Footer.LegalText}</p>");
        sb.AppendLine("    </footer>");

        sb.AppendLine("  </div>");

        return sb.ToString();
    }

    public static string GenerateCss(TemplateBuilderModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine(":root {");
        sb.AppendLine("  color-scheme: light;");
        sb.AppendLine($"  --primary: {model.Theme.PrimaryColor};");
        sb.AppendLine($"  --secondary: {model.Theme.SecondaryColor};");
        sb.AppendLine("  --accent: #3182ce;");
        sb.AppendLine("  --text-main: #223145;");
        sb.AppendLine("  --text-muted: #556376;");
        sb.AppendLine("  --line: #d8e1ec;");
        sb.AppendLine("  --bg-alt: #f6f9fc;");
        sb.AppendLine($"  --margin-top: {model.Layout.TopMargin}px;");
        sb.AppendLine($"  --margin-bottom: {model.Layout.BottomMargin}px;");
        sb.AppendLine($"  --margin-left: {model.Layout.LeftMargin}px;");
        sb.AppendLine($"  --margin-right: {model.Layout.RightMargin}px;");
        sb.AppendLine($"  --header-padding-top: {model.Layout.HeaderPaddingTop}mm;");
        sb.AppendLine($"  --header-padding-bottom: {model.Layout.HeaderPaddingBottom}mm;");
        sb.AppendLine($"  --footer-padding-top: {model.Layout.FooterPaddingTop}px;");
        sb.AppendLine($"  --footer-padding-bottom: {model.Layout.FooterPaddingBottom}px;");
        sb.AppendLine($"  --section-margin-bottom: {model.Layout.SectionMarginBottom}px;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("* { box-sizing: border-box; }");
        sb.AppendLine();
        sb.AppendLine("body {");
        sb.AppendLine("  margin: 0; padding: 0; background: #e2e2e2; color: var(--text-main);");
        sb.AppendLine($"  font-family: \"{model.Theme.FontFamily}\", \"Helvetica Neue\", Arial, sans-serif;");
        sb.AppendLine("  font-size: 10pt; line-height: 1.45;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine(".page { max-width: 210mm; width: 100%; min-height: 297mm; margin: 0 auto; background: #fff; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1); position: relative; display: block; overflow: hidden; }");
        sb.AppendLine(".header-banner { background: linear-gradient(180deg, var(--secondary) 0%, var(--primary) 100%); color: #fff; padding: var(--header-padding-top) var(--margin-right) var(--header-padding-bottom) var(--margin-left); }");
        sb.AppendLine(".header-top-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; border-bottom: 1px solid rgba(255, 255, 255, 0.28); padding-bottom: 8px; margin-bottom: 10px; }");
        sb.AppendLine(".logo-wrap { min-height: 30px; display: flex; align-items: center; background: transparent; }");
        sb.AppendLine(".logo-wrap img.logo-image { width: auto; object-fit: contain; display: block; background: transparent; }");
        sb.AppendLine(".doc-meta { text-align: right; font-size: 8.8pt; color: #d5e4f5; line-height: 1.35; }");
        sb.AppendLine(".doc-meta strong { display: block; color: #9ac7f4; font-size: 9.2pt; margin-bottom: 2px; text-transform: uppercase; letter-spacing: 0.06em; }");
        sb.AppendLine(".product-title { margin: 0; font-size: 18pt; font-weight: 700; text-transform: uppercase; line-height: 1.2; letter-spacing: 0.06em; }");
        sb.AppendLine(".product-subtitle { margin: 4px 0 10px; color: #b7d7f6; font-size: 11pt; font-weight: 500; }");
        sb.AppendLine(".product-description { margin: 0; max-width: 78%; color: #e6f0fb; text-align: justify; font-size: 10pt; line-height: 1.5; }");
        sb.AppendLine(".main-content { padding: 10mm var(--margin-right) 13mm var(--margin-left); min-height: 200mm; }");
        sb.AppendLine(".batch-note { margin: 0 0 12px; padding: 8px 10px; border-left: 3px solid var(--accent); background: #ebf6ff; color: #1f4f7d; font-size: 9pt; }");
        sb.AppendLine(".two-column { display: flex; flex-wrap: wrap; gap: 16px; align-items: flex-start; }");
        sb.AppendLine(".two-column > .section { flex: 0 1 calc(50% - 8px); width: calc(50% - 8px); box-sizing: border-box; }");
        sb.AppendLine(".section { page-break-inside: avoid; break-inside: avoid; margin-bottom: var(--section-margin-bottom); }");
        sb.AppendLine(".section.full-width { width: 100%; }");
        sb.AppendLine(".section-title { margin: 0 0 8px; font-size: 10pt; text-transform: uppercase; color: var(--secondary); border-bottom: 2px solid var(--line); padding-bottom: 4px; font-weight: 700; letter-spacing: 0.07em; }");
        sb.AppendLine(".features-grid { display: grid; gap: 6px; font-size: 9.6pt; }");
        sb.AppendLine(".feature-item { display: flex; align-items: flex-start; gap: 7px; line-height: 1.35; }");
        sb.AppendLine(".feature-check { flex-shrink: 0; width: 15px; height: 15px; margin-top: 1px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; background: var(--primary); color: #fff; font-size: 9px; font-weight: 700; }");
        sb.AppendLine("table.data-table { width: 100%; border-collapse: collapse; font-size: 9.1pt; }");
        sb.AppendLine("table.data-table th, table.data-table td { border: 1px solid var(--line); padding: 5px 7px; vertical-align: top; }");
        sb.AppendLine("table.data-table th { width: 44%; text-align: left; color: var(--primary); background: #fff; font-weight: 600; }");
        sb.AppendLine("table.data-table td { text-align: left; color: var(--text-main); }");
        sb.AppendLine("table.data-table tr:nth-child(even) th, table.data-table tr:nth-child(even) td { background: var(--bg-alt); }");
        sb.AppendLine(".full-width { column-span: all; }");
        sb.AppendLine(".chart-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 10px; }");
        sb.AppendLine(".chart-block { border: 1px solid var(--line); background: #fff; border-radius: 3px; padding: 8px; width: 100%; break-inside: avoid; }");
        sb.AppendLine(".chart-block h4 { margin: 0 0 6px; color: var(--secondary); font-size: 9pt; font-weight: 600; }");
        sb.AppendLine(".chart-block img, .chart-block svg, .chart-block figure { max-width: 100%; height: auto; display: block; }");
        sb.AppendLine(".footer-area { padding: var(--footer-padding-top) var(--margin-right) var(--footer-padding-bottom) var(--margin-left); border-top: 1px solid var(--line); background: #fff; }");
        sb.AppendLine(".footer-cols { display: flex; gap: 20px; font-size: 8.5pt; font-weight: 600; color: var(--primary); margin-bottom: 4px; }");
        sb.AppendLine(".footer-legal { margin: 10px 0 0; color: var(--text-muted); font-size: 8pt; line-height: 1.35; text-align: justify; }");
        sb.AppendLine(".page-break-preview { display: flex; align-items: center; justify-content: center; margin: 18px 0; border-top: 1px dashed var(--line); position: relative; }");
        sb.AppendLine(".page-break-preview span { background: #fff; padding: 0 8px; font-size: 7.5pt; color: var(--text-muted); font-weight: bold; text-transform: uppercase; letter-spacing: 0.1em; }");
        sb.AppendLine("/* Custom inheritance rules when section specifies custom styles */");
        sb.AppendLine(".section[style*=\"font-family\"] .section-title, .section[style*=\"font-family\"] .features-grid, .section[style*=\"font-family\"] table.data-table, .section[style*=\"font-family\"] table.data-table th, .section[style*=\"font-family\"] table.data-table td, .section[style*=\"font-family\"] .chart-block h4 { font-family: inherit; }");
        sb.AppendLine(".section[style*=\"color\"] .section-title { color: inherit; border-bottom-color: currentColor; }");
        sb.AppendLine(".section[style*=\"color\"] .features-grid, .section[style*=\"color\"] table.data-table, .section[style*=\"color\"] table.data-table th, .section[style*=\"color\"] table.data-table td, .section[style*=\"color\"] .chart-block h4 { color: inherit; }");
        sb.AppendLine(".section[style*=\"font-size\"] .section-title { font-size: 1.15em; }");
        sb.AppendLine(".section[style*=\"font-size\"] .features-grid, .section[style*=\"font-size\"] table.data-table, .section[style*=\"font-size\"] table.data-table th, .section[style*=\"font-size\"] table.data-table td, .section[style*=\"font-size\"] .chart-block h4 { font-size: inherit; }");
        sb.AppendLine("@media print { body { background: #fff; } .page { box-shadow: none; margin: 0; width: 100%; min-height: auto; } @page { size: A4; margin: 0; } .header-banner { padding: var(--header-padding-top) var(--margin-right) var(--header-padding-bottom) var(--margin-left); } .main-content { padding: 10mm var(--margin-right) 13mm var(--margin-left); } .footer-area { padding: var(--footer-padding-top) var(--margin-right) var(--footer-padding-bottom) var(--margin-left); } .page-break-preview { display: none; } }");
        
        return sb.ToString();
    }

    private static void GenerateSectionHtml(StringBuilder sb, TemplateSection sec)
    {
        string cssClass = sec.IsHalfWidth ? "section" : "section full-width";

        var styles = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(sec.FontFamily))
            styles.Add($"font-family: '{sec.FontFamily}', sans-serif;");
        if (!string.IsNullOrWhiteSpace(sec.TextColor))
            styles.Add($"color: {sec.TextColor};");
        if (sec.FontSize > 0)
            styles.Add($"font-size: {sec.FontSize}pt;");
        if (sec.MarginBottom >= 0)
            styles.Add($"margin-bottom: {sec.MarginBottom}px;");
        if (sec.MinHeight > 0)
            styles.Add($"min-height: {sec.MinHeight}px;");

        string secStyle = styles.Count > 0 ? $" style=\"{string.Join(" ", styles)}\"" : "";

        if (!string.IsNullOrWhiteSpace(sec.DataSource) && sec.Type != "RichText" && sec.Type != "Charts")
        {
            sb.AppendLine($"        {{{{#if {sec.DataSource}}}}}");
        }

        sb.AppendLine($"        <section class=\"{cssClass}\"{secStyle}>");
        sb.AppendLine($"          <h2 class=\"section-title\">{sec.Title}</h2>");

        if (sec.Type == "RichText")
        {
            sb.AppendLine($"          {sec.Content}");
        }
        else if (sec.Type == "BulletPoints")
        {
            sb.AppendLine("          <div class=\"features-grid\">");
            sb.AppendLine($"            {{{{#each {sec.DataSource}}}}}");
            sb.AppendLine("            <div class=\"feature-item\"><span class=\"feature-check\">✓</span><span>{{this}}</span></div>");
            sb.AppendLine("            {{/each}}");
            sb.AppendLine("          </div>");
        }
        else if (sec.Type == "Table")
        {
            sb.AppendLine("          <table class=\"data-table\">");
            sb.AppendLine("            <tbody>");
            sb.AppendLine($"              <!-- {{{{#each {sec.DataSource}}}}} -->");
            sb.AppendLine("              <tr><th>{{@key}}</th><td>{{this}}</td></tr>");
            sb.AppendLine("              <!-- {{/each}} -->");
            sb.AppendLine("            </tbody>");
            sb.AppendLine("          </table>");
        }
        else if (sec.Type == "Charts")
        {
            sb.AppendLine("          {{#if Charts}}");
            sb.AppendLine("          <div class=\"chart-grid\">");
            sb.AppendLine("            {{#if Charts.HasLinearity}}");
            sb.AppendLine("            <div class=\"chart-block\"><h4>Linearity</h4>{{{Charts.LinearitySvg}}}</div>");
            sb.AppendLine("            {{/if}}");
            sb.AppendLine("            {{#if Charts.HasResistance}}");
            sb.AppendLine("            <div class=\"chart-block\"><h4>Resistance vs Force</h4>{{{Charts.ResistanceSvg}}}</div>");
            sb.AppendLine("            {{/if}}");
            sb.AppendLine("            {{#if Charts.HasHysteresis}}");
            sb.AppendLine("            <div class=\"chart-block\"><h4>Hysteresis</h4>{{{Charts.HysteresisSvg}}}</div>");
            sb.AppendLine("            {{/if}}");
            sb.AppendLine("          </div>");
            sb.AppendLine("          {{/if}}");
        }

        sb.AppendLine("        </section>");

        if (!string.IsNullOrWhiteSpace(sec.DataSource) && sec.Type != "RichText" && sec.Type != "Charts")
        {
            sb.AppendLine("        {{/if}}");
        }
    }
}
