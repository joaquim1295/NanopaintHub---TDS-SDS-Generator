using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NanopaintHub.Application.Tds;

public static class TemplateHtmlParser
{
    public static TemplateBuilderSchema ParseToSchema(string html, string css)
    {
        var schema = new TemplateBuilderSchema();

        if (string.IsNullOrWhiteSpace(html) && string.IsNullOrWhiteSpace(css))
            return schema;

        html = UnprotectMce(html);

        // If CSS is empty, try to extract it from <style> tags in HTML
        if (string.IsNullOrWhiteSpace(css) && !string.IsNullOrWhiteSpace(html))
        {
            var styleMatch = Regex.Match(html, @"<style[^>]*>([\s\S]*?)</style>", RegexOptions.IgnoreCase);
            if (styleMatch.Success)
            {
                css = styleMatch.Groups[1].Value;
            }
        }

        // Parse CSS
        if (!string.IsNullOrWhiteSpace(css))
        {
            var primaryMatch = Regex.Match(css, @"--primary:\s*([^;}\s]+)");
            if (primaryMatch.Success) schema.Theme.PrimaryColor = NormalizeColor(primaryMatch.Groups[1].Value.Trim());

            var secondaryMatch = Regex.Match(css, @"--secondary:\s*([^;}\s]+)");
            if (secondaryMatch.Success) schema.Theme.SecondaryColor = NormalizeColor(secondaryMatch.Groups[1].Value.Trim());

            var fontMatch = Regex.Match(css, @"font-family:\s*""([^""]+)""");
            if (!fontMatch.Success)
                fontMatch = Regex.Match(css, @"font-family:\s*'([^']+)'");
            if (!fontMatch.Success)
                fontMatch = Regex.Match(css, @"font-family:\s*([^;},\s]+)");
            if (fontMatch.Success) schema.Theme.FontFamily = fontMatch.Groups[1].Value.Trim();

            // Page layout settings from CSS custom properties
            var topMarginMatch = Regex.Match(css, @"--margin-top:\s*(\d+(?:\.\d+)?)(px|mm)");
            if (topMarginMatch.Success && double.TryParse(topMarginMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var tm))
                schema.Layout.TopMargin = tm;

            var bottomMarginMatch = Regex.Match(css, @"--margin-bottom:\s*(\d+(?:\.\d+)?)(px|mm)");
            if (bottomMarginMatch.Success && double.TryParse(bottomMarginMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var bm))
                schema.Layout.BottomMargin = bm;

            var leftMarginMatch = Regex.Match(css, @"--margin-left:\s*(\d+(?:\.\d+)?)(px|mm)");
            if (leftMarginMatch.Success && double.TryParse(leftMarginMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var lm))
                schema.Layout.LeftMargin = lm;

            var rightMarginMatch = Regex.Match(css, @"--margin-right:\s*(\d+(?:\.\d+)?)(px|mm)");
            if (rightMarginMatch.Success && double.TryParse(rightMarginMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var rm))
                schema.Layout.RightMargin = rm;

            var headerPadTopMatch = Regex.Match(css, @"--header-padding-top:\s*(\d+(?:\.\d+)?)mm");
            if (headerPadTopMatch.Success && double.TryParse(headerPadTopMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var hpt))
                schema.Layout.HeaderPaddingTop = hpt;

            var headerPadBottomMatch = Regex.Match(css, @"--header-padding-bottom:\s*(\d+(?:\.\d+)?)mm");
            if (headerPadBottomMatch.Success && double.TryParse(headerPadBottomMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var hpb))
                schema.Layout.HeaderPaddingBottom = hpb;

            var footerPadTopMatch = Regex.Match(css, @"--footer-padding-top:\s*(\d+(?:\.\d+)?)px");
            if (footerPadTopMatch.Success && double.TryParse(footerPadTopMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var fpt))
                schema.Layout.FooterPaddingTop = fpt;

            var footerPadBottomMatch = Regex.Match(css, @"--footer-padding-bottom:\s*(\d+(?:\.\d+)?)px");
            if (footerPadBottomMatch.Success && double.TryParse(footerPadBottomMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var fpb))
                schema.Layout.FooterPaddingBottom = fpb;

            var contentPadTopMatch = Regex.Match(css, @"--content-padding-top:\s*(\d+(?:\.\d+)?)mm");
            if (contentPadTopMatch.Success && double.TryParse(contentPadTopMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var cpt))
                schema.Layout.ContentPaddingTop = cpt;

            var batchPadBottomMatch = Regex.Match(css, @"--batch-note-padding-bottom:\s*(\d+(?:\.\d+)?)mm");
            if (batchPadBottomMatch.Success && double.TryParse(batchPadBottomMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var bnpb))
                schema.Layout.BatchNotePaddingBottom = bnpb;

            var secSpacingMatch = Regex.Match(css, @"--section-spacing:\s*(\d+(?:\.\d+)?)px");
            if (secSpacingMatch.Success && double.TryParse(secSpacingMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var ssp))
                schema.Layout.SectionSpacing = ssp;
            else
            {
                var secMarginMatch = Regex.Match(css, @"--section-margin-bottom:\s*(\d+(?:\.\d+)?)px");
                if (secMarginMatch.Success && double.TryParse(secMarginMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var smb))
                {
                    schema.Layout.SectionSpacing = smb;
                    schema.Layout.SectionMarginBottom = smb;
                }
            }

            // Header Color / Font from CSS
            var headerColorMatch = Regex.Match(css, @"\.header-banner\s*\{[^}]*color:\s*([^;}]+)");
            if (headerColorMatch.Success) schema.Header.TextColor = NormalizeColor(headerColorMatch.Groups[1].Value.Trim());

            var headerFontMatch = Regex.Match(css, @"\.header-banner\s*\{[^}]*font-family:\s*([^;}]+)");
            if (headerFontMatch.Success) schema.Header.FontFamily = headerFontMatch.Groups[1].Value.Replace("\"", "").Replace("'", "").Trim();

            // Footer Color / Font from CSS
            var footerColorMatch = Regex.Match(css, @"\.footer-cols\s*\{[^}]*color:\s*([^;}]+)");
            if (!footerColorMatch.Success)
                footerColorMatch = Regex.Match(css, @"\.footer-contact\s*\{[^}]*color:\s*([^;}]+)");
            if (!footerColorMatch.Success)
                footerColorMatch = Regex.Match(css, @"\.footer-area\s*\{[^}]*color:\s*([^;}]+)");
            if (footerColorMatch.Success) schema.Footer.TextColor = ResolveCssColor(footerColorMatch.Groups[1].Value.Trim(), schema);

            var footerFontMatch = Regex.Match(css, @"\.footer-area\s*\{[^}]*font-family:\s*([^;}]+)");
            if (footerFontMatch.Success) schema.Footer.FontFamily = footerFontMatch.Groups[1].Value.Replace("\"", "").Replace("'", "").Trim();
        }

        // Parse HTML
        if (!string.IsNullOrWhiteSpace(html))
        {
            // Parse Header Fields
            var headerStyleMatch = Regex.Match(html, @"<header[^>]*style=""([^""]+)""");
            if (headerStyleMatch.Success)
            {
                var style = headerStyleMatch.Groups[1].Value;
                var fm = Regex.Match(style, @"font-family:\s*['""]?([^'"",;}]+)");
                if (fm.Success) schema.Header.FontFamily = fm.Groups[1].Value.Trim();
                var cm = Regex.Match(style, @"color:\s*([^;]+)");
                if (cm.Success) schema.Header.TextColor = NormalizeColor(cm.Groups[1].Value.Trim());
                var sm = Regex.Match(style, @"font-size:\s*([\d\.]+)pt");
                if (sm.Success && double.TryParse(sm.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double size))
                    schema.Header.FontSize = size;
            }

            var logoTagMatch = Regex.Match(html, @"<img\s+[^>]*logo-image[^>]*>");
            if (!logoTagMatch.Success)
            {
                logoTagMatch = Regex.Match(html, @"<div class=""logo-wrap"">\s*<img\s+([^>]+)>");
            }

            if (logoTagMatch.Success)
            {
                var attrs = logoTagMatch.Value;
                var srcMatch = Regex.Match(attrs, @"src=""([^""]+)""");
                
                string logoUrl = "";
                if (srcMatch.Success)
                    logoUrl = srcMatch.Groups[1].Value;
                    
                schema.Header.LogoUrl = (logoUrl == "{{Assets.Logo}}" || logoUrl == "{{{Assets.Logo}}}") ? "" : logoUrl;
                
                var heightStyleMatch = Regex.Match(attrs, @"height:\s*(\d+(?:\.\d+)?)px");
                var heightAttrMatch = Regex.Match(attrs, @"height=""(\d+(?:\.\d+)?)""");
                
                if (heightStyleMatch.Success && double.TryParse(heightStyleMatch.Groups[1].Value, CultureInfo.InvariantCulture, out double hs))
                    schema.Header.LogoHeight = (int)Math.Round(hs);
                else if (heightAttrMatch.Success && double.TryParse(heightAttrMatch.Groups[1].Value, CultureInfo.InvariantCulture, out double ha))
                    schema.Header.LogoHeight = (int)Math.Round(ha);
            }
            else schema.Header.ShowLogo = false;

            var categoryMatch = Regex.Match(html, @"<strong>(.*?)</strong>\s*(.*?)<br");
            if (categoryMatch.Success)
            {
                schema.Header.InfoCategory = StripHtmlTags(categoryMatch.Groups[1].Value);
                schema.Header.InfoDocType = StripHtmlTags(categoryMatch.Groups[2].Value);
            }

            var revisionMatch = Regex.Match(html, @"Revised:\s*(.*?)\s*·\s*(.*?)\s*</div>");
            if (revisionMatch.Success)
            {
                var dateStr = revisionMatch.Groups[1].Value.Trim();
                if (dateStr == "{{RevisionDate}}") {
                    schema.Header.InfoRevisedDate = null;
                }
                else if (DateTimeOffset.TryParse(dateStr, out var parsedDate))
                {
                    schema.Header.InfoRevisedDate = parsedDate;
                }
                else
                {
                    schema.Header.InfoRevisedDate = null;
                }
                
                schema.Header.InfoVersion = StripHtmlTags(revisionMatch.Groups[2].Value);
            }

            var typeMatch = Regex.Match(html, @"<h1 class=""product-title"">(.*?)</h1>");
            if (typeMatch.Success) {
                schema.Header.IdentType = StripHtmlTags(UnprotectMce(typeMatch.Groups[1].Value));
            }

            var nameMatch = Regex.Match(html, @"<p class=""product-subtitle"">(.*?)</p>");
            if (nameMatch.Success) {
                schema.Header.IdentName = StripHtmlTags(UnprotectMce(nameMatch.Groups[1].Value));
            }

            var descMatch = Regex.Match(html, @"<p class=""product-description"">(.*?)</p>");
            if (descMatch.Success) {
                schema.Header.IdentDesc = StripHtmlTags(UnprotectMce(descMatch.Groups[1].Value));
            }

            // Fallback font families if empty (inherit from body/theme font)
            if (string.IsNullOrWhiteSpace(schema.Header.FontFamily))
                schema.Header.FontFamily = schema.Theme.FontFamily;
            if (string.IsNullOrWhiteSpace(schema.Footer.FontFamily))
                schema.Footer.FontFamily = schema.Theme.FontFamily;


            // Parse Structured Sections
            var sectionMatches = Regex.Matches(html, @"<section\s+([^>]*)>([\s\S]*?)</section>");
            if (sectionMatches.Count > 0)
            {
                int lastIndex = 0;
                foreach (Match match in sectionMatches)
                {
                    var attrs = match.Groups[1].Value;
                    var innerHtml = match.Groups[2].Value;

                    var classMatch = Regex.Match(attrs, @"class=""([^""]+)""");
                    var cssClass = classMatch.Success ? classMatch.Groups[1].Value : "";

                    var section = new TemplateSectionSchema
                    {
                        Id = Guid.NewGuid().ToString(),
                        IsHalfWidth = !cssClass.Contains("full-width")
                    };

                    // Look for page break before the section
                    var gapHtml = html.Substring(lastIndex, match.Index - lastIndex);
                    section.PageBreakBefore = gapHtml.Contains("page-break-before: always") || gapHtml.Contains("break-before: page");
                    lastIndex = match.Index + match.Length;

                    var styleMatch = Regex.Match(attrs, @"style=""([^""]+)""");
                    if (styleMatch.Success)
                    {
                        var style = styleMatch.Groups[1].Value;
                        var fm = Regex.Match(style, @"font-family:\s*['""]?([^'"",;}]+)");
                        if (fm.Success) section.FontFamily = fm.Groups[1].Value.Trim();
                        var cm = Regex.Match(style, @"color:\s*([^;]+)");
                        if (cm.Success) section.TextColor = cm.Groups[1].Value.Trim();

                        var fsMatch = Regex.Match(style, @"font-size:\s*(\d+(?:\.\d+)?)\s*pt");
                        if (fsMatch.Success && double.TryParse(fsMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var fs))
                            section.FontSize = fs;

                        var mbMatch = Regex.Match(style, @"margin-bottom:\s*(\d+(?:\.\d+)?)\s*px");
                        if (mbMatch.Success && double.TryParse(mbMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var mb))
                            section.MarginBottom = mb;

                        var mhMatch = Regex.Match(style, @"min-height:\s*(\d+(?:\.\d+)?)\s*px");
                        if (mhMatch.Success && double.TryParse(mhMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var mh))
                            section.MinHeight = mh;
                    }

                    var titleMatch = Regex.Match(innerHtml, @"<h2[^>]*>(.*?)</h2>");
                    if (titleMatch.Success)
                        section.Title = StripHtmlTags(titleMatch.Groups[1].Value);

                    if (innerHtml.Contains("features-grid"))
                    {
                        var ds = ExtractDataSource(innerHtml, "{{#each", "}}");
                        if (!string.IsNullOrWhiteSpace(ds))
                        {
                            section.Type = "BulletPoints";
                            section.DataSource = ds;
                        }
                        else
                        {
                            section.Type = "RichText";
                            var content = Regex.Replace(innerHtml, @"<h2[^>]*>.*?</h2>", "");
                            section.Content = content.Trim();
                        }
                    }
                    else if (innerHtml.Contains("data-table"))
                    {
                        var ds = ExtractDataSource(innerHtml, "{{#each", "}}");
                        if (!string.IsNullOrWhiteSpace(ds))
                        {
                            section.Type = "Table";
                            section.DataSource = ds;
                        }
                        else
                        {
                            section.Type = "RichText";
                            var content = Regex.Replace(innerHtml, @"<h2[^>]*>.*?</h2>", "");
                            section.Content = content.Trim();
                        }
                    }
                    else if (innerHtml.Contains("chart-grid"))
                    {
                        section.Type = "Charts";
                    }
                    else
                    {
                        section.Type = "RichText";
                        var content = Regex.Replace(innerHtml, @"<h2[^>]*>.*?</h2>", "");
                        section.Content = content.Trim();
                    }

                    schema.Sections.Add(section);
                }
            }
            else
            {
                var bodyMatch = Regex.Match(html, @"<body>([\s\S]*?)</body>");
                string bodyContent = bodyMatch.Success ? bodyMatch.Groups[1].Value : html;
                bodyContent = Regex.Replace(bodyContent, @"<div class=""page"">([\s\S]*?)</div>", "$1");

                schema.Sections.Add(new TemplateSectionSchema
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Imported Content",
                    Type = "RichText",
                    Content = bodyContent.Trim(),
                    IsHalfWidth = false
                });
            }

            var footerMatch = Regex.Match(html, @"<footer[^>]*>([\s\S]*?)</footer>");
            var footerStyleMatch = Regex.Match(html, @"<footer[^>]*style=""([^""]+)""");
            if (footerStyleMatch.Success)
            {
                var style = footerStyleMatch.Groups[1].Value;
                var fm = Regex.Match(style, @"font-family:\s*['""]?([^'"",;}]+)");
                if (fm.Success) schema.Footer.FontFamily = fm.Groups[1].Value.Trim();
                var cm = Regex.Match(style, @"color:\s*([^;]+)");
                if (cm.Success) schema.Footer.TextColor = cm.Groups[1].Value.Trim();
            }

            if (footerMatch.Success)
            {
                var fHtml = footerMatch.Groups[1].Value;
                var legalMatch = Regex.Match(fHtml, @"<p class=""footer-legal"">(.*?)</p>", RegexOptions.Singleline);
                if (legalMatch.Success) schema.Footer.LegalText = StripHtmlTags(legalMatch.Groups[1].Value);

                var col1Match = Regex.Match(fHtml, @"<div class=""footer-col1"">(.*?)</div>", RegexOptions.Singleline);
                if (col1Match.Success) schema.Footer.Column1 = StripHtmlTags(col1Match.Groups[1].Value);

                var col2Match = Regex.Match(fHtml, @"<div class=""footer-col2"">(.*?)</div>", RegexOptions.Singleline);
                if (col2Match.Success) schema.Footer.Column2 = StripHtmlTags(col2Match.Groups[1].Value);
            }
        }

        return schema;
    }

    private static string ExtractDataSource(string html, string prefix, string suffix)
    {
        var match = Regex.Match(html, $@"{prefix}\s+([A-Za-z0-9_]+)\s*{suffix}");
        return match.Success ? match.Groups[1].Value : "";
    }

    private static string UnprotectMce(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return Regex.Replace(input, @"<!--mce:protected\s+([^>]+)-->", m =>
        {
            try
            {
                return Uri.UnescapeDataString(m.Groups[1].Value);
            }
            catch { return m.Value; }
        });
    }

    private static string StripHtmlTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var temp = Regex.Replace(input, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        temp = Regex.Replace(temp, @"<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(temp).Trim();
    }

    private static string NormalizeColor(string color)
    {
        if (string.IsNullOrEmpty(color)) return "";
        color = color.Trim();
        if (string.Equals(color, "#fff", StringComparison.OrdinalIgnoreCase)) return "#ffffff";
        if (string.Equals(color, "#000", StringComparison.OrdinalIgnoreCase)) return "#000000";
        return color;
    }

    private static string ResolveCssColor(string colorValue, TemplateBuilderSchema schema)
    {
        if (string.IsNullOrEmpty(colorValue)) return "";
        colorValue = colorValue.Trim();
        if (colorValue == "var(--primary)")
            return schema.Theme.PrimaryColor;
        if (colorValue == "var(--secondary)")
            return schema.Theme.SecondaryColor;
        return NormalizeColor(colorValue);
    }
}
