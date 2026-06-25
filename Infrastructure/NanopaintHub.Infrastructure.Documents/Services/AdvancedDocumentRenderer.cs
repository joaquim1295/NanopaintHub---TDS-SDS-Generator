using System.Text;
using System.Text.Json;
using NanopaintHub.Application.Tds;
using HandlebarsDotNet;

namespace NanopaintHub.Infrastructure.Documents.Services;

public static class AdvancedDocumentRenderer
{
    public static string Render(TemplateBuilderSchema model, object templateData)
    {
        var htmlTemplate = GenerateHandlebarsTemplate(model);
        var compiled = Handlebars.Compile(htmlTemplate);
        return compiled(templateData);
    }

    private static string GenerateHandlebarsTemplate(TemplateBuilderSchema model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine($"<style>\n{GenerateCss(model)}\n</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"page-wrapper\">");

        // Header Minimal (Runs into Paged.js top margin)
        if (model.Header.HeaderRepeatStyle != "None")
        {
            sb.AppendLine("    <div class=\"header-minimal\">");
            sb.AppendLine("      <div class=\"minimal-header-left\">");
            if (model.Header.ShowLogo)
            {
                var logoUrl = string.IsNullOrWhiteSpace(model.Header.LogoUrl) ? "{{Assets.Logo}}" : model.Header.LogoUrl;
                sb.AppendLine($"        <img src=\"{logoUrl}\" alt=\"Logo\" style=\"height:16px; object-fit:contain;\" />");
            }
            sb.AppendLine("        <span class=\"minimal-title\">{{TemplateName}}</span>");
            sb.AppendLine("      </div>");
            var infoTypeStr = string.IsNullOrWhiteSpace(model.Header.InfoDocType) ? "Technical Data Sheet" : model.Header.InfoDocType;
            sb.AppendLine($"      <div class=\"minimal-doctype\">{infoTypeStr}</div>");
            sb.AppendLine("    </div>");
        }

        // Footer Area (Runs into Paged.js bottom margin)
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

        // Header Banner (Sits on the first page body)
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

        sb.AppendLine("    <main class=\"main-content\">");
        sb.AppendLine("      {{#if HasBatch}}");
        sb.AppendLine("      <p class=\"batch-note\">Batch data applied: <strong>{{BatchNumber}}</strong> (merged with product specifications where applicable).</p>");
        sb.AppendLine("      {{/if}}");

        var halfWidthQueue = new System.Collections.Generic.List<TemplateSectionSchema>();

        void FlushHalfWidthQueue()
        {
            if (halfWidthQueue.Count > 0)
            {
                sb.AppendLine("      <div class=\"masonry-row\" style=\"display: flex; gap: 16px; align-items: flex-start; width: 100%;\">");
                
                // Left Column
                sb.AppendLine("        <div class=\"masonry-col\" style=\"flex: 1; min-width: 0;\">");
                for (int i = 0; i < halfWidthQueue.Count; i += 2)
                {
                    GenerateSectionHtml(sb, halfWidthQueue[i], model);
                }
                sb.AppendLine("        </div>");

                // Right Column
                sb.AppendLine("        <div class=\"masonry-col\" style=\"flex: 1; min-width: 0;\">");
                for (int i = 1; i < halfWidthQueue.Count; i += 2)
                {
                    GenerateSectionHtml(sb, halfWidthQueue[i], model);
                }
                sb.AppendLine("        </div>");
                
                sb.AppendLine("      </div>");
                halfWidthQueue.Clear();
            }
        }

        foreach (var sec in model.Sections)
        {
            if (sec.PageBreakBefore)
            {
                FlushHalfWidthQueue();
                sb.AppendLine("      <div class=\"page-break-preview\"><span>--- QUEBRA DE PÁGINA ---</span></div>");
                sb.AppendLine("      <div style=\"break-before: page;\"></div>");
            }

            if (sec.IsHalfWidth)
            {
                halfWidthQueue.Add(sec);
            }
            else
            {
                FlushHalfWidthQueue();
                GenerateSectionHtml(sb, sec, model);
            }
        }

        FlushHalfWidthQueue();
        sb.AppendLine("    </main>");

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string GenerateCss(TemplateBuilderSchema model)
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
        sb.AppendLine($"  --margin-top: {model.Layout.TopMargin}mm;");
        sb.AppendLine($"  --margin-bottom: {model.Layout.BottomMargin}mm;");
        sb.AppendLine($"  --margin-left: {model.Layout.LeftMargin}mm;");
        sb.AppendLine($"  --margin-right: {model.Layout.RightMargin}mm;");
        sb.AppendLine($"  --section-margin-bottom: {model.Layout.SectionMarginBottom}px;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("* { box-sizing: border-box; }");
        sb.AppendLine();
        sb.AppendLine("body {");
        sb.AppendLine("  margin: 0; padding: 0; background: #fff; color: var(--text-main);");
        sb.AppendLine($"  font-family: \"{model.Theme.FontFamily}\", \"Segoe UI\", Arial, sans-serif;");
        sb.AppendLine("  font-size: 10pt; line-height: 1.45;");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine(".header-minimal { position: running(header-running); border-bottom: 2px solid var(--primary); padding: 0 var(--margin-right) 2mm var(--margin-left); display: flex; justify-content: space-between; align-items: flex-end; font-size: 8.5pt; color: var(--secondary); font-weight: 600; width: 100%; box-sizing: border-box; }");
        sb.AppendLine(".minimal-header-left { display: flex; align-items: center; gap: 8px; }");
        sb.AppendLine(".minimal-title { font-weight: bold; color: var(--primary); }");
        sb.AppendLine(".minimal-doctype { color: var(--secondary); font-weight: 500; }");

        sb.AppendLine(".footer-area { position: running(footer-running); padding: 6mm var(--margin-right) 4mm var(--margin-left); border-top: 1px solid var(--line); width: 100%; }");
        sb.AppendLine(".footer-cols { display: flex; justify-content: space-between; font-size: 8pt; color: var(--primary); font-weight: 600; margin-bottom: 4px; }");
        sb.AppendLine(".footer-col2 { text-align: right; }");
        sb.AppendLine(".footer-legal { margin: 0; color: var(--text-muted); font-size: 8pt; line-height: 1.35; text-align: justify; }");

        sb.AppendLine(".header-banner { background: linear-gradient(180deg, var(--secondary) 0%, var(--primary) 100%); color: #fff; padding: 12mm var(--margin-right) 12mm var(--margin-left); margin-bottom: 10mm; }");
        sb.AppendLine(".header-top-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; border-bottom: 1px solid rgba(255, 255, 255, 0.28); padding-bottom: 8px; margin-bottom: 10px; }");
        sb.AppendLine(".logo-wrap { min-height: 30px; display: flex; align-items: center; background: transparent; }");
        sb.AppendLine(".logo-wrap img.logo-image { max-height: 34px; width: auto; object-fit: contain; display: block; }");
        sb.AppendLine(".doc-meta { text-align: right; font-size: 8.8pt; color: #d5e4f5; line-height: 1.35; }");
        sb.AppendLine(".doc-meta strong { display: block; color: #9ac7f4; letter-spacing: 0.06em; text-transform: uppercase; font-size: 9.2pt; margin-bottom: 2px; }");
        sb.AppendLine(".product-title { margin: 0; font-size: 18pt; font-weight: 700; letter-spacing: 0.06em; text-transform: uppercase; line-height: 1.2; }");
        sb.AppendLine(".product-subtitle { margin: 4px 0 10px; color: #b7d7f6; font-size: 11pt; font-weight: 500; }");
        sb.AppendLine(".product-description { margin: 0; max-width: 78%; color: #e6f0fb; text-align: justify; font-size: 10pt; line-height: 1.5; }");

        sb.AppendLine(".main-content { padding: 0 var(--margin-right) 0 var(--margin-left); box-sizing: border-box; }");
        sb.AppendLine(".batch-note { margin: 0 0 12px; padding: 8px 10px; border-left: 3px solid var(--accent); background: #ebf6ff; color: #1f4f7d; font-size: 9pt; }");
        
        sb.AppendLine(".two-column { column-count: 2; column-gap: 16px; display: block; }");
        sb.AppendLine(".section { break-inside: avoid; page-break-inside: avoid; margin-bottom: var(--section-margin-bottom); width: 100%; }");
        sb.AppendLine(".full-width { width: 100%; }");
        
        sb.AppendLine(".section-title { margin: 0 0 8px; font-size: 10pt; letter-spacing: 0.07em; text-transform: uppercase; color: var(--secondary); border-bottom: 2px solid var(--line); padding-bottom: 4px; font-weight: 700; }");
        sb.AppendLine(".features-grid { display: grid; gap: 6px; font-size: 9.6pt; }");
        sb.AppendLine(".feature-item { display: flex; align-items: flex-start; gap: 7px; line-height: 1.35; }");
        sb.AppendLine(".feature-check { flex-shrink: 0; width: 15px; height: 15px; margin-top: 1px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; background: var(--primary); color: #fff; font-size: 9px; font-weight: 700; }");
        
        sb.AppendLine("table.data-table { width: 100%; border-collapse: collapse; font-size: 9.1pt; }");
        sb.AppendLine("table.data-table th, table.data-table td { border: 1px solid var(--line); padding: 5px 7px; vertical-align: top; }");
        sb.AppendLine("table.data-table th { width: 44%; text-align: left; color: var(--primary); background: #fff; font-weight: 600; }");
        sb.AppendLine("table.data-table td { text-align: left; color: var(--text-main); }");
        sb.AppendLine("table.data-table tr:nth-child(even) th, table.data-table tr:nth-child(even) td { background: var(--bg-alt); }");
        
        sb.AppendLine(".chart-grid { display: flex; flex-wrap: wrap; gap: 10px; }");
        sb.AppendLine(".chart-block { flex: 1 1 160px; border: 1px solid var(--line); background: #fff; border-radius: 3px; padding: 8px; width: 100%; break-inside: avoid; }");
        sb.AppendLine(".chart-block h4 { margin: 0 0 6px; color: var(--secondary); font-size: 9pt; font-weight: 600; }");
        sb.AppendLine(".chart-block img, .chart-block svg, .chart-block figure { max-width: 100%; height: auto; display: block; }");

        sb.AppendLine("/* === PAGED.JS PRINT CSS === */");
        sb.AppendLine("@media print { ");
        sb.AppendLine("  body { background: #fff; } ");
        sb.AppendLine("  .page-wrapper { margin: 0; width: 100%; }");
        sb.AppendLine("  @page {");
        sb.AppendLine("    size: A4;");
        sb.AppendLine("    margin: var(--margin-top) 0 var(--margin-bottom) 0;");
        sb.AppendLine("    @top-left { content: none; }");
        sb.AppendLine("    @top-right { content: none; }");
        sb.AppendLine("    @top-center { content: element(header-running); width: 100%; }");
        sb.AppendLine("    @bottom-left { content: none; }");
        sb.AppendLine("    @bottom-right { content: none; }");
        sb.AppendLine("    @bottom-center { content: element(footer-running); width: 100%; }");
        sb.AppendLine("  }");
        sb.AppendLine("  @page :first { margin-top: 0; @top-center { content: none; } }");
        sb.AppendLine("}");
        
        sb.AppendLine("/* === SCREEN PREVIEW CSS === */");
        sb.AppendLine("@media screen { ");
        sb.AppendLine("  body { padding: 20px; background: #e2e2e2; } ");
        sb.AppendLine("  .page-wrapper { width: 210mm; min-height: 297mm; margin: 0 auto; background: #fff; box-shadow: 0 4px 15px rgba(0,0,0,0.1); position: relative; padding: var(--margin-top) 0 var(--margin-bottom) 0; box-sizing: border-box; }");
        sb.AppendLine("  .header-minimal { display: none; }");
        sb.AppendLine("  .footer-area { position: absolute; bottom: 0; left: 0; width: 100%; border-top: 1px solid var(--line); padding: 6mm var(--margin-right); box-sizing: border-box; }");
        sb.AppendLine("  .header-banner { margin-top: calc(-1 * var(--margin-top)); }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static void GenerateSectionHtml(StringBuilder sb, TemplateSectionSchema sec, TemplateBuilderSchema model)
    {
        string cssClass = sec.IsHalfWidth ? "section" : "section full-width";

        var styles = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(sec.FontFamily))
            styles.Add($"font-family: '{sec.FontFamily}', sans-serif;");
        if (!string.IsNullOrWhiteSpace(sec.TextColor))
            styles.Add($"color: {sec.TextColor};");
        if (sec.FontSize > 0)
            styles.Add($"font-size: {sec.FontSize}pt;");
        else if (model.Layout.SectionBaseScale != 1.0)
            styles.Add($"font-size: {model.Layout.SectionBaseScale.ToString(System.Globalization.CultureInfo.InvariantCulture)}em;");
            
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
            if (!string.IsNullOrWhiteSpace(sec.DataSource))
            {
                sb.AppendLine($"            {{{{#each {sec.DataSource}}}}}");
                sb.AppendLine("            <div class=\"feature-item\"><span class=\"feature-check\">✓</span><span>{{this}}</span></div>");
                sb.AppendLine("            {{/each}}");
            }
            sb.AppendLine("          </div>");
        }
        else if (sec.Type == "Table")
        {
            sb.AppendLine("          <table class=\"data-table\">");
            sb.AppendLine("            <tbody>");
            if (!string.IsNullOrWhiteSpace(sec.DataSource))
            {
                sb.AppendLine($"              <!-- {{{{#each {sec.DataSource}}}}} -->");
                sb.AppendLine("              <tr><th>{{@key}}</th><td>{{this}}</td></tr>");
                sb.AppendLine("              <!-- {{/each}} -->");
            }
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
