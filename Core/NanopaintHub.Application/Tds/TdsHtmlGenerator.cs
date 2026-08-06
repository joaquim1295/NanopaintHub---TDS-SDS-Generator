using System;
using System.Text;
using NanopaintHub.Application.Contracts;

namespace NanopaintHub.Application.Tds;

public sealed class TdsHtmlGenerator : ITdsHtmlGenerator
{
    public string GenerateHtml(TemplateBuilderSchema schema)
    {
        var sb = new StringBuilder();

        sb.AppendLine("  <div class=\"page\">");

        // Header
        string headerStyle = "";
        if (!string.IsNullOrWhiteSpace(schema.Header.FontFamily) || !string.IsNullOrWhiteSpace(schema.Header.TextColor) || schema.Header.FontSize > 0)
        {
            var f = !string.IsNullOrWhiteSpace(schema.Header.FontFamily) ? $"font-family: '{schema.Header.FontFamily}', sans-serif;" : "";
            var c = !string.IsNullOrWhiteSpace(schema.Header.TextColor) ? $"color: {schema.Header.TextColor};" : "";
            var s = schema.Header.FontSize > 0 ? $"font-size: {schema.Header.FontSize}pt;" : "";
            headerStyle = $" style=\"{f} {c} {s}\"";
        }
        sb.AppendLine($"    <header class=\"header-banner\"{headerStyle}>");
        sb.AppendLine("      <div class=\"header-top-row\">");
        sb.AppendLine("        <div class=\"logo-wrap\">");
        if (schema.Header.ShowLogo)
        {
            var logoUrl = string.IsNullOrWhiteSpace(schema.Header.LogoUrl) ? "{{Assets.Logo}}" : schema.Header.LogoUrl;
            sb.AppendLine($"          <img class=\"logo-image\" src=\"{logoUrl}\" alt=\"Logo\" height=\"{schema.Header.LogoHeight}\" />");
        }
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"doc-meta\">");
        sb.AppendLine($"          <strong>{{{{Category}}}}</strong>");
        string safeSubtitle = System.Net.WebUtility.HtmlEncode(schema.Header.DocumentSubtitle);
        if (!string.IsNullOrWhiteSpace(safeSubtitle))
        {
            sb.AppendLine($"          {safeSubtitle}<br />");
        }
        sb.AppendLine($"          Revised: {{{{RevisionDate}}}} · Version {{{{Version}}}}");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>");
        
        var flexAlign = string.Equals(schema.Header.ProductImageAlignment, "Center", StringComparison.OrdinalIgnoreCase) ? "center"
            : string.Equals(schema.Header.ProductImageAlignment, "Bottom", StringComparison.OrdinalIgnoreCase) ? "flex-end"
            : "flex-start";

        sb.AppendLine($"      <div class=\"product-main-header-row\" style=\"display:flex; justify-content:space-between; align-items:{flexAlign}; gap:16px; margin-top:12px;\">");
        sb.AppendLine("        <div class=\"product-main-info\" style=\"flex:1;\">");
        sb.AppendLine($"          <h1 class=\"product-title\" style=\"margin:0 0 4px 0;\">{{{{ProductType}}}}</h1>");
        sb.AppendLine($"          <p class=\"product-subtitle\" style=\"margin:0 0 8px 0;\">{{{{Name}}}} · {{{{Code}}}}</p>");
        sb.AppendLine("          <p class=\"product-description\" style=\"margin:0;\">{{Description}}</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        {{#if Assets.ProductImage}}");
        sb.AppendLine($"        <img class=\"product-header-image\" src=\"{{Assets.ProductImage}}\" alt=\"Product Image\" style=\"max-height:{schema.Header.ProductImageMaxHeight}px; width:auto; object-fit:contain; margin-top:{schema.Header.ProductImageMarginTop}px; flex-shrink:0;\" />");
        sb.AppendLine("        {{/if}}");
        sb.AppendLine("      </div>");
        sb.AppendLine("    </header>");

        // Main Content in Table
        sb.AppendLine("    <table class=\"main-layout-table\">");
        if (schema.Header.HeaderRepeatStyle != "None")
        {
            sb.AppendLine("      <thead>");
            sb.AppendLine("        <tr>");
            sb.AppendLine("          <td class=\"minimal-header-cell\">");
            sb.AppendLine("            <div class=\"minimal-header\">");
            sb.AppendLine("              <div class=\"minimal-header-left\">");
            if (schema.Header.ShowLogo)
            {
                var logoUrl = string.IsNullOrWhiteSpace(schema.Header.LogoUrl) ? "{{Assets.Logo}}" : schema.Header.LogoUrl;
                sb.AppendLine($"                <img src=\"{logoUrl}\" alt=\"Logo\" />");
            }
            sb.AppendLine("                <span class=\"minimal-title\">{{TemplateName}}</span>");
            sb.AppendLine("              </div>");
            sb.AppendLine("              <div class=\"minimal-doctype\">");
            sb.AppendLine($"                <div>{{DocumentKind}}</div>");
            sb.AppendLine($"                <div class=\"minimal-rev\">Rev: {{{{Version}}}} | {{{{RevisionDate}}}}</div>");
            sb.AppendLine("              </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("          </td>");
            sb.AppendLine("        </tr>");
            sb.AppendLine("      </thead>");
        }
        sb.AppendLine("      <tbody>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("          <td class=\"main-content-cell\">");
        sb.AppendLine("            <main class=\"main-content\">");
        sb.AppendLine("      {{#if HasBatch}}");
        sb.AppendLine("      <p class=\"batch-note\">Batch data applied: <strong>{{BatchNumber}}</strong> (merged with product specifications where applicable).</p>");
        sb.AppendLine("      {{/if}}");
        var halfWidthQueue = new System.Collections.Generic.List<TemplateSectionSchema>();

        void FlushHalfWidthQueue()
        {
            if (halfWidthQueue.Count > 0)
            {
                for (int i = 0; i < halfWidthQueue.Count; i += 2)
                {
                    sb.AppendLine("      <div class=\"section-row-two-col\" style=\"display: flex; gap: 16px; align-items: flex-start; width: 100%; margin-bottom: 16px;\">");
                    sb.AppendLine("        <div class=\"masonry-col\" style=\"flex: 1; min-width: 0;\">");
                    GenerateSectionHtml(sb, halfWidthQueue[i], schema);
                    sb.AppendLine("        </div>");
                    sb.AppendLine("        <div class=\"masonry-col\" style=\"flex: 1; min-width: 0;\">");
                    if (i + 1 < halfWidthQueue.Count)
                    {
                        GenerateSectionHtml(sb, halfWidthQueue[i + 1], schema);
                    }
                    sb.AppendLine("        </div>");
                    sb.AppendLine("      </div>");
                }
                halfWidthQueue.Clear();
            }
        }

        foreach (var sec in schema.Sections)
        {
            if (sec.PageBreakBefore)
            {
                FlushHalfWidthQueue();
                sb.AppendLine("      <div class=\"page-break-preview\"><span>--- QUEBRA DE PÁGINA ---</span></div>");
                sb.AppendLine("      <div style=\"page-break-before: always; break-before: page;\"></div>");
            }

            if (sec.IsHalfWidth)
            {
                halfWidthQueue.Add(sec);
            }
            else
            {
                FlushHalfWidthQueue();
                GenerateSectionHtml(sb, sec, schema);
            }
        }

        FlushHalfWidthQueue();

        sb.AppendLine("            </main>");
        sb.AppendLine("          </td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");

        // Footer
        string footerStyle = "";
        if (!string.IsNullOrWhiteSpace(schema.Footer.FontFamily) || !string.IsNullOrWhiteSpace(schema.Footer.TextColor))
        {
            var f = !string.IsNullOrWhiteSpace(schema.Footer.FontFamily) ? $"font-family: '{schema.Footer.FontFamily}', sans-serif;" : "";
            var c = !string.IsNullOrWhiteSpace(schema.Footer.TextColor) ? $"color: {schema.Footer.TextColor};" : "";
            footerStyle = $" style=\"{f} {c}\"";
        }
        sb.AppendLine($"    <footer class=\"footer-area\"{footerStyle}>");
        sb.AppendLine("      <div class=\"footer-cols\">");
        sb.AppendLine($"        <div class=\"footer-col1\">{(schema.Footer.Column1 ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br/>")}</div>");
        sb.AppendLine($"        <div class=\"footer-col2\">{(schema.Footer.Column2 ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br/>")}</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <p class=\"footer-legal\">{(schema.Footer.LegalText ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br/>")}</p>");
        sb.AppendLine("    </footer>");

        sb.AppendLine("  </div>");

        return sb.ToString();
    }

    public string GenerateCss(TemplateBuilderSchema schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine(":root {");
        sb.AppendLine("  color-scheme: light;");
        sb.AppendLine($"  --primary: {schema.Theme.PrimaryColor};");
        sb.AppendLine($"  --secondary: {schema.Theme.SecondaryColor};");
        sb.AppendLine("  --accent: #3182ce;");
        sb.AppendLine("  --text-main: #223145;");
        sb.AppendLine("  --text-muted: #556376;");
        sb.AppendLine("  --line: #d8e1ec;");
        sb.AppendLine("  --bg-alt: #f6f9fc;");
        sb.AppendLine($"  --margin-top: {schema.Layout.TopMargin.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --margin-bottom: {schema.Layout.BottomMargin.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --margin-left: {schema.Layout.LeftMargin.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --margin-right: {schema.Layout.RightMargin.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --header-padding-top: {schema.Layout.HeaderPaddingTop.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --header-padding-bottom: {schema.Layout.HeaderPaddingBottom.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --header-padding-left: {schema.Layout.HeaderPaddingLeft.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --header-padding-right: {schema.Layout.HeaderPaddingRight.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --content-padding-top: {schema.Layout.ContentPaddingTop.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --batch-note-padding-bottom: {schema.Layout.BatchNotePaddingBottom.ToString(System.Globalization.CultureInfo.InvariantCulture)}mm;");
        sb.AppendLine($"  --footer-padding-top: {schema.Layout.FooterPaddingTop.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;");
        sb.AppendLine($"  --footer-padding-bottom: {schema.Layout.FooterPaddingBottom.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;");
        sb.AppendLine($"  --section-spacing: {schema.Layout.SectionSpacing.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("*:empty { display: none !important; }");
        sb.AppendLine();
        sb.AppendLine(".minimal-title {");
        sb.AppendLine("  font-size: 6pt;");
        sb.AppendLine("  color: #777;");
        sb.AppendLine("  margin-top: 2px;");
        sb.AppendLine("}");
        sb.AppendLine(".main-content {");
        sb.AppendLine("  margin: 0; padding: 0; background: #e2e2e2; color: var(--text-main);");
        sb.AppendLine($"  font-family: \"{schema.Theme.FontFamily}\", \"Helvetica Neue\", Arial, sans-serif;");
        var baseSize = schema.Theme.BaseFontSize > 0 ? schema.Theme.BaseFontSize : 10;
        sb.AppendLine($"  font-size: {baseSize}pt; line-height: 1.45;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("* { box-sizing: border-box; }");
        sb.AppendLine();
        sb.AppendLine("body {");
        sb.AppendLine("  margin: 0; padding: 0; background: #e2e2e2; color: var(--text-main);");
        sb.AppendLine($"  font-family: \"{schema.Theme.FontFamily}\", \"Helvetica Neue\", Arial, sans-serif;");
        sb.AppendLine($"  font-size: {baseSize}pt; line-height: 1.45;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine(".page { max-width: 210mm; width: 100%; min-height: 297mm; margin: 0 auto; background: #fff; box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1); position: relative; display: block; overflow: hidden; }");
        sb.AppendLine(".header-banner { background: linear-gradient(180deg, var(--secondary) 0%, var(--primary) 100%); color: #fff; padding: var(--header-padding-top) var(--header-padding-right) var(--header-padding-bottom) var(--header-padding-left); }");
        sb.AppendLine(".header-top-row { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; border-bottom: 1px solid rgba(255, 255, 255, 0.28); padding-bottom: 8px; margin-bottom: 10px; }");
        sb.AppendLine(".logo-wrap { min-height: 30px; display: flex; align-items: center; background: transparent; }");
        sb.AppendLine(".logo-wrap img.logo-image { width: auto; object-fit: contain; display: block; background: transparent; }");
        sb.AppendLine(".doc-meta { text-align: right; font-size: 0.88em; color: #d5e4f5; line-height: 1.35; }");
        sb.AppendLine(".doc-meta strong { display: block; color: #9ac7f4; font-size: 0.92em; margin-bottom: 2px; text-transform: uppercase; letter-spacing: 0.06em; }");
        sb.AppendLine(".product-title { margin: 0; font-size: 1.8em; font-weight: 700; text-transform: uppercase; line-height: 1.2; letter-spacing: 0.06em; }");
        sb.AppendLine(".product-subtitle { margin: 4px 0 10px; color: #b7d7f6; font-size: 1.1em; font-weight: 500; }");
        sb.AppendLine(".product-description { margin: 0; font-size: 0.95em; line-height: 1.4; color: #d5e4f5; max-width: 85%; }");
        sb.AppendLine(".main-layout-table { width: 100%; border-collapse: collapse; margin-top: calc(-1 * var(--margin-top)); border: none; }");
        sb.AppendLine(".minimal-header-cell { height: var(--margin-top); padding: 0; vertical-align: bottom; border: none; }");
        sb.AppendLine(".main-content-cell { padding: 0; border: none; }");
        sb.AppendLine(".minimal-header { display: flex; justify-content: space-between; align-items: flex-end; padding: 0 var(--margin-right) 4px var(--margin-left); border-bottom: 1px solid var(--line); font-size: 0.8em; color: var(--text-muted); box-sizing: border-box; }");
        sb.AppendLine(".minimal-header-left { display: flex; align-items: center; gap: 8px; }");
        sb.AppendLine(".minimal-header-left img { height: 16px; width: auto; object-fit: contain; }");
        sb.AppendLine(".minimal-title { font-weight: bold; color: var(--primary); }");
        sb.AppendLine(".minimal-doctype { color: var(--secondary); font-weight: 500; }");
        sb.AppendLine(".main-content { padding: var(--content-padding-top) var(--margin-right) 13mm var(--margin-left); min-height: 200mm; }");
        sb.AppendLine(".batch-note { background: #f0f6fc; color: #1f4f7d; padding: 10px 14px; margin: 0 0 var(--batch-note-padding-bottom); font-size: 0.85em; font-weight: 500; border-left: 3px solid var(--accent); }");
        sb.AppendLine(".two-column { column-count: 2; column-gap: 16px; display: block; column-fill: auto; }");
        sb.AppendLine(".section { page-break-inside: avoid; break-inside: avoid; margin-bottom: var(--section-spacing); width: 100%; }");
        sb.AppendLine(".section.full-width { width: 100%; }");
        sb.AppendLine(".section-title { margin: 0 0 8px; font-size: 1.05em; text-transform: uppercase; color: var(--secondary); border-bottom: 2px solid var(--line); padding-bottom: 4px; font-weight: 700; letter-spacing: 0.07em; }");
        sb.AppendLine(".features-grid { display: grid; gap: 6px; font-size: 0.96em; }");
        sb.AppendLine(".feature-item { display: flex; align-items: flex-start; gap: 7px; line-height: 1.35; }");
        sb.AppendLine(".feature-check { flex-shrink: 0; width: 15px; height: 15px; margin-top: 1px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; background: var(--primary); color: #fff; font-size: 0.6em; font-weight: 700; }");
        sb.AppendLine("table.data-table { width: 100%; border-collapse: collapse; font-size: 0.9em; }");
        sb.AppendLine("table.data-table th, table.data-table td { border: 1px solid var(--line); padding: 5px 7px; vertical-align: top; }");
        sb.AppendLine("table.data-table tr { page-break-inside: avoid; break-inside: avoid; }");
        sb.AppendLine("table.data-table th { width: 44%; text-align: left; color: var(--primary); background: #fff; font-weight: 600; }");
        sb.AppendLine("table.data-table td { text-align: left; color: var(--text-main); }");
        sb.AppendLine("table.data-table tr:nth-child(even) th, table.data-table tr:nth-child(even) td { background: var(--bg-alt); }");
        sb.AppendLine(".full-width { column-span: all; }");
        sb.AppendLine(".chart-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 10px; }");
        sb.AppendLine(".chart-block { border: 1px solid var(--line); background: #fff; border-radius: 3px; padding: 8px; width: 100%; break-inside: avoid; }");
        sb.AppendLine(".chart-block h4 { margin: 0 0 6px; color: var(--secondary); font-size: 0.9em; font-weight: 600; }");
        sb.AppendLine(".chart-block img, .chart-block svg, .chart-block figure { max-width: 100%; height: auto; display: block; }");
        sb.AppendLine(".footer-area { padding: 6mm var(--margin-right) 4mm var(--margin-left); border-top: 1px solid var(--line); background: #fff; }");
        sb.AppendLine(".footer-cols { display: flex; gap: 20px; font-size: 0.85em; font-weight: 600; color: var(--primary); margin-bottom: 4px; }");
        sb.AppendLine(".footer-legal { margin: 25px 0 0; color: var(--text-muted); font-size: 0.8em; line-height: 1.35; text-align: justify; }");
        sb.AppendLine(".page-break-preview { display: flex; align-items: center; justify-content: center; margin: 18px 0; border-top: 1px dashed var(--line); position: relative; }");
        sb.AppendLine(".page-break-preview span { background: #fff; padding: 0 8px; font-size: 0.75em; color: var(--text-muted); font-weight: bold; text-transform: uppercase; letter-spacing: 0.1em; }");
        sb.AppendLine("/* Custom inheritance rules when section specifies custom styles */");
        sb.AppendLine(".section[style*=\"font-family\"] .section-title, .section[style*=\"font-family\"] .features-grid, .section[style*=\"font-family\"] table.data-table, .section[style*=\"font-family\"] table.data-table th, .section[style*=\"font-family\"] table.data-table td, .section[style*=\"font-family\"] .chart-block h4 { font-family: inherit; }");
        sb.AppendLine(".section[style*=\"color\"] .section-title { color: inherit; border-bottom-color: currentColor; }");
        sb.AppendLine(".section[style*=\"color\"] .features-grid, .section[style*=\"color\"] table.data-table, .section[style*=\"color\"] table.data-table th, .section[style*=\"color\"] table.data-table td, .section[style*=\"color\"] .chart-block h4 { color: inherit; }");
        if (schema.Layout.HeaderBaseScale != 1.0)
        {
            var headerScaleStr = schema.Layout.HeaderBaseScale.ToString(System.Globalization.CultureInfo.InvariantCulture);
            sb.AppendLine($".header-banner {{ font-size: {headerScaleStr}em; }}");
            sb.AppendLine($".header-banner .logo-image {{ transform: scale({headerScaleStr}); transform-origin: left top; }}");
        }
        sb.AppendLine("@page:first { margin-top: 0px; }");
        sb.AppendLine("@media print { ");
        sb.AppendLine("  body { background: #fff; } ");
        sb.AppendLine("  .page { box-shadow: none; margin: 0; width: 100%; min-height: auto; }");
        sb.AppendLine("  .header-banner { position: relative; z-index: 10; padding: var(--header-padding-top) var(--header-padding-right) var(--header-padding-bottom) var(--header-padding-left); } ");
        sb.AppendLine("  .section { page-break-inside: avoid; break-inside: avoid; }");
        sb.AppendLine("  table { page-break-inside: auto; }");
        sb.AppendLine("  tr { page-break-inside: avoid; page-break-after: auto; }");
        sb.AppendLine("  .header-banner { padding: var(--header-padding-top) var(--header-padding-right) var(--header-padding-bottom) var(--header-padding-left); } ");
        sb.AppendLine("  .main-content { padding: var(--content-padding-top) var(--margin-right) 13mm var(--margin-left); } ");
        sb.AppendLine("  .footer-area { padding: 6mm var(--margin-right) 4mm var(--margin-left); } ");
        sb.AppendLine("  .footer-legal { display: none; } ");
        sb.AppendLine("  .page-break-preview { display: none; } ");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private void GenerateSectionHtml(StringBuilder sb, TemplateSectionSchema sec, TemplateBuilderSchema schema)
    {
        string cssClass = sec.IsHalfWidth ? "section" : "section full-width";

        var styles = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(sec.FontFamily))
            styles.Add($"font-family: '{sec.FontFamily}', sans-serif;");
        if (!string.IsNullOrWhiteSpace(sec.TextColor))
            styles.Add($"color: {sec.TextColor};");
        if (sec.FontSize > 0)
            styles.Add($"font-size: {sec.FontSize}pt;");
        else if (schema.Layout.SectionBaseScale != 1.0)
            styles.Add($"font-size: {schema.Layout.SectionBaseScale.ToString(System.Globalization.CultureInfo.InvariantCulture)}em;");
            
        if (sec.MarginBottom >= 0)
            styles.Add($"margin-bottom: {sec.MarginBottom}px;");
        if (sec.MinHeight > 0)
            styles.Add($"min-height: {sec.MinHeight}px;");

        string secStyle = styles.Count > 0 ? $" style=\"{string.Join(" ", styles)}\"" : "";
        string dataAttrs = $" data-type=\"{sec.Type}\" data-source=\"{sec.DataSource}\"";

        bool isSensorSec = sec.DataSource == "Tds" || sec.DataSource == "Sensor" ||
                           string.Equals(sec.Title, "Sensor specifications", System.StringComparison.OrdinalIgnoreCase) ||
                           (sec.Content != null && sec.Content.Contains("{{Tds."));

        if (isSensorSec)
        {
            sb.AppendLine("        {{#if Tds}}");
        }
        else if (!string.IsNullOrWhiteSpace(sec.DataSource) && sec.Type != "RichText" && sec.Type != "Charts")
        {
            sb.AppendLine($"        {{{{#if {sec.DataSource}}}}}");
        }

        sb.AppendLine($"        <section class=\"{cssClass}\"{secStyle}{dataAttrs}>");
        sb.AppendLine($"          <h2 class=\"section-title\">{sec.Title}</h2>");

        if (!string.IsNullOrWhiteSpace(sec.Content) && (sec.Type == "RichText" || sec.Type == "SdsSectionText" || (sec.Type != null && sec.Type.StartsWith("SdsSection"))))
        {
            sb.AppendLine($"          {sec.Content}");
        }
        else if (sec.Type == "RichText")
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

        if (isSensorSec)
        {
            sb.AppendLine("        {{/if}}");
        }
        else if (!string.IsNullOrWhiteSpace(sec.DataSource) && sec.Type != "RichText" && sec.Type != "Charts")
        {
            sb.AppendLine("        {{/if}}");
        }
    }
}
