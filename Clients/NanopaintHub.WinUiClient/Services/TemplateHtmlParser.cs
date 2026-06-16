using System;
using System.Text.RegularExpressions;
using NanopaintHub_WinUIClient.Models;

namespace NanopaintHub_WinUIClient.Services;

public static class TemplateHtmlParser
{
    public static TemplateBuilderModel Parse(string html, string css)
    {
        var model = new TemplateBuilderModel();

        if (string.IsNullOrWhiteSpace(html) && string.IsNullOrWhiteSpace(css))
            return model;

        // Parse CSS
        if (!string.IsNullOrWhiteSpace(css))
        {
            var primaryMatch = Regex.Match(css, @"--primary:\s*(#[0-9a-fA-F]{3,6})");
            if (primaryMatch.Success) model.Theme.PrimaryColor = primaryMatch.Groups[1].Value;

            var secondaryMatch = Regex.Match(css, @"--secondary:\s*(#[0-9a-fA-F]{3,6})");
            if (secondaryMatch.Success) model.Theme.SecondaryColor = secondaryMatch.Groups[1].Value;

            var fontMatch = Regex.Match(css, @"font-family:\s*""([^""]+)""");
            if (fontMatch.Success) model.Theme.FontFamily = fontMatch.Groups[1].Value;
        }

        // Parse HTML
        if (!string.IsNullOrWhiteSpace(html))
        {
            // Parse Header Fields
            var headerStyleMatch = Regex.Match(html, @"<header[^>]*style=""([^""]+)""");
            if (headerStyleMatch.Success)
            {
                var style = headerStyleMatch.Groups[1].Value;
                var fm = Regex.Match(style, @"font-family:\s*'([^']+)'");
                if (fm.Success) model.Header.FontFamily = fm.Groups[1].Value;
                var cm = Regex.Match(style, @"color:\s*(#[0-9a-fA-F]{3,6}|rgb\([^\)]+\)|[a-zA-Z]+)");
                if (cm.Success) model.Header.TextColor = cm.Groups[1].Value;
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
                    
                model.Header.LogoUrl = (logoUrl == "{{Assets.Logo}}" || logoUrl == "{{{Assets.Logo}}}") ? "" : logoUrl;
                
                var heightStyleMatch = Regex.Match(attrs, @"height:\s*(\d+(?:\.\d+)?)px");
                var heightAttrMatch = Regex.Match(attrs, @"height=""(\d+(?:\.\d+)?)""");
                
                if (heightStyleMatch.Success && double.TryParse(heightStyleMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out double hs))
                    model.Header.LogoHeight = (int)Math.Round(hs);
                else if (heightAttrMatch.Success && double.TryParse(heightAttrMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out double ha))
                    model.Header.LogoHeight = (int)Math.Round(ha);
            }
            else model.Header.ShowLogo = false;

            var categoryMatch = Regex.Match(html, @"<strong>(.*?)</strong>\s*(.*?)<br");
            if (categoryMatch.Success)
            {
                var cat = categoryMatch.Groups[1].Value.Trim();
                model.Header.InfoCategory = cat == "Functional Inks" ? "" : cat;
                var docType = categoryMatch.Groups[2].Value.Trim();
                model.Header.InfoDocType = docType == "Technical Data Sheet" ? "" : docType;
            }

            var revisionMatch = Regex.Match(html, @"Revised:\s*(.*?)\s*·\s*(.*?)\s*</div>");
            if (revisionMatch.Success)
            {
                var dateStr = revisionMatch.Groups[1].Value.Trim();
                if (dateStr == "{{RevisionDate}}") {
                    model.Header.InfoRevisedDate = null;
                }
                else if (DateTimeOffset.TryParse(dateStr, out var parsedDate))
                {
                    model.Header.InfoRevisedDate = parsedDate;
                }
                else
                {
                    model.Header.InfoRevisedDate = null;
                }
                
                var ver = revisionMatch.Groups[2].Value.Trim();
                model.Header.InfoVersion = ver == "Version {{Version}}" ? "" : ver;
            }

            var typeMatch = Regex.Match(html, @"<h1 class=""product-title"">(.*?)</h1>");
            if (typeMatch.Success) {
                var val = UnprotectMce(typeMatch.Groups[1].Value.Trim());
                model.Header.IdentType = val == "Piezoresistive Ink" ? "" : val;
            }

            var nameMatch = Regex.Match(html, @"<p class=""product-subtitle"">(.*?)</p>");
            if (nameMatch.Success) {
                var val = UnprotectMce(nameMatch.Groups[1].Value.Trim());
                model.Header.IdentName = (val == "{{Name}}" || val == "{{Name}} · {{Code}}") ? "" : val;
            }

            var descMatch = Regex.Match(html, @"<p class=""product-description"">(.*?)</p>");
            if (descMatch.Success) {
                var val = UnprotectMce(descMatch.Groups[1].Value.Trim());
                model.Header.IdentDesc = val == "{{Description}}" ? "" : val;
            }


            // Parse Structured Sections
            var sectionMatches = Regex.Matches(html, @"<section\s+([^>]*)>([\s\S]*?)</section>");
            if (sectionMatches.Count > 0)
            {
                foreach (Match match in sectionMatches)
                {
                    var attrs = match.Groups[1].Value;
                    var innerHtml = match.Groups[2].Value;

                    var classMatch = Regex.Match(attrs, @"class=""([^""]+)""");
                    var cssClass = classMatch.Success ? classMatch.Groups[1].Value : "";

                    var section = new TemplateSection
                    {
                        IsHalfWidth = !cssClass.Contains("full-width")
                    };

                    var styleMatch = Regex.Match(attrs, @"style=""([^""]+)""");
                    if (styleMatch.Success)
                    {
                        var style = styleMatch.Groups[1].Value;
                        var fm = Regex.Match(style, @"font-family:\s*'([^']+)'");
                        if (fm.Success) section.FontFamily = fm.Groups[1].Value;
                        var cm = Regex.Match(style, @"color:\s*(#[0-9a-fA-F]{3,6}|rgb\([^\)]+\)|[a-zA-Z]+)");
                        if (cm.Success) section.TextColor = cm.Groups[1].Value;
                    }

                    var titleMatch = Regex.Match(innerHtml, @"<h2[^>]*>(.*?)</h2>");
                    if (titleMatch.Success)
                        section.Title = titleMatch.Groups[1].Value.Trim();

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

                    model.Sections.Add(section);
                }
            }
            else
            {
                var bodyMatch = Regex.Match(html, @"<body>([\s\S]*?)</body>");
                string bodyContent = bodyMatch.Success ? bodyMatch.Groups[1].Value : html;
                bodyContent = Regex.Replace(bodyContent, @"<div class=""page"">([\s\S]*?)</div>", "$1");

                model.Sections.Add(new TemplateSection
                {
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
                var fm = Regex.Match(style, @"font-family:\s*'([^']+)'");
                if (fm.Success) model.Footer.FontFamily = fm.Groups[1].Value;
                var cm = Regex.Match(style, @"color:\s*(#[0-9a-fA-F]{3,6}|rgb\([^\)]+\)|[a-zA-Z]+)");
                if (cm.Success) model.Footer.TextColor = cm.Groups[1].Value;
            }

            if (footerMatch.Success)
            {
                var fHtml = footerMatch.Groups[1].Value;
                var legalMatch = Regex.Match(fHtml, @"<p class=""footer-legal"">(.*?)</p>", RegexOptions.Singleline);
                if (legalMatch.Success) model.Footer.LegalText = legalMatch.Groups[1].Value.Trim();

                var col1Match = Regex.Match(fHtml, @"<div class=""footer-col1"">(.*?)</div>", RegexOptions.Singleline);
                if (col1Match.Success) model.Footer.Column1 = col1Match.Groups[1].Value.Replace("<br/>", "\n").Trim();

                var col2Match = Regex.Match(fHtml, @"<div class=""footer-col2"">(.*?)</div>", RegexOptions.Singleline);
                if (col2Match.Success) model.Footer.Column2 = col2Match.Groups[1].Value.Replace("<br/>", "\n").Trim();
            }
        }

        return model;
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
}
