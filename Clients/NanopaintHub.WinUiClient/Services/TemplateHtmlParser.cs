using System;
using NanopaintHub.Application.Tds;
using NanopaintHub_WinUIClient.Models;

namespace NanopaintHub_WinUIClient.Services;

public static class TemplateHtmlParser
{
    public static TemplateBuilderModel Parse(string html, string css)
    {
        var schema = NanopaintHub.Application.Tds.TemplateHtmlParser.ParseToSchema(html, css);
        return MapSchemaToModel(schema);
    }

    private static TemplateBuilderModel MapSchemaToModel(TemplateBuilderSchema schema)
    {
        var model = new TemplateBuilderModel();

        if (schema.Theme != null)
        {
            model.Theme.PrimaryColor = schema.Theme.PrimaryColor;
            model.Theme.SecondaryColor = schema.Theme.SecondaryColor;
            model.Theme.BodyTextColor = string.IsNullOrWhiteSpace(schema.Theme.BodyTextColor) ? "#223145" : schema.Theme.BodyTextColor;
            model.Theme.TitleFontFamily = string.IsNullOrWhiteSpace(schema.Theme.TitleFontFamily) ? "Segoe UI" : schema.Theme.TitleFontFamily;
            model.Theme.FontFamily = schema.Theme.FontFamily;
            model.Theme.BaseFontSize = schema.Theme.BaseFontSize;
        }

        if (schema.Header != null)
        {
            model.Header.LogoUrl = schema.Header.LogoUrl;
            model.Header.LogoHeight = schema.Header.LogoHeight;
            model.Header.ShowLogo = schema.Header.ShowLogo;
            model.Header.FontFamily = schema.Header.FontFamily;
            model.Header.TextColor = schema.Header.TextColor;
            model.Header.HeaderRepeatStyle = schema.Header.HeaderRepeatStyle;
            model.Header.DocumentSubtitle = schema.Header.DocumentSubtitle;

            if (schema.Header.RepeatingHeader != null)
            {
                model.Header.RepeatingHeader.IsEnabled = schema.Header.RepeatingHeader.IsEnabled;
                model.Header.RepeatingHeader.ShowLogo = schema.Header.RepeatingHeader.ShowLogo;
                model.Header.RepeatingHeader.LogoHeight = schema.Header.RepeatingHeader.LogoHeight;
                model.Header.RepeatingHeader.BackgroundColor = schema.Header.RepeatingHeader.BackgroundColor ?? "";
                model.Header.RepeatingHeader.TextColor = schema.Header.RepeatingHeader.TextColor ?? "#19528d";
                model.Header.RepeatingHeader.ShowBorderBottom = schema.Header.RepeatingHeader.ShowBorderBottom;
                model.Header.RepeatingHeader.BorderColor = schema.Header.RepeatingHeader.BorderColor ?? "#19528d";
                model.Header.RepeatingHeader.ShowRevisionInfo = schema.Header.RepeatingHeader.ShowRevisionInfo;
            }
        }

        if (schema.Footer != null)
        {
            model.Footer.Column1 = schema.Footer.Column1;
            model.Footer.Column2 = schema.Footer.Column2;
            model.Footer.LegalText = schema.Footer.LegalText;
            model.Footer.FontFamily = schema.Footer.FontFamily;
            model.Footer.TextColor = schema.Footer.TextColor;
            model.Footer.FooterRepeatStyle = schema.Footer.FooterRepeatStyle;
        }

        if (schema.Batch != null)
        {
            model.Batch.ShowBatchNote = schema.Batch.ShowBatchNote;
            model.Batch.BatchNoteFormat = schema.Batch.BatchNoteFormat;
        }

        if (schema.Layout != null)
        {
            model.Layout.TopMargin = (int)schema.Layout.TopMargin;
            model.Layout.BottomMargin = (int)schema.Layout.BottomMargin;
            model.Layout.LeftMargin = (int)schema.Layout.LeftMargin;
            model.Layout.RightMargin = (int)schema.Layout.RightMargin;
            model.Layout.HeaderPaddingTop = (int)schema.Layout.HeaderPaddingTop;
            model.Layout.HeaderPaddingBottom = (int)schema.Layout.HeaderPaddingBottom;
            model.Layout.HeaderPaddingLeft = (int)schema.Layout.HeaderPaddingLeft;
            model.Layout.HeaderPaddingRight = (int)schema.Layout.HeaderPaddingRight;
            model.Layout.ContentPaddingTop = (int)schema.Layout.ContentPaddingTop;
            model.Layout.BatchNotePaddingBottom = (int)schema.Layout.BatchNotePaddingBottom;
            model.Layout.SectionSpacing = (int)schema.Layout.SectionSpacing;
            model.Layout.TitleBottomSpacing = schema.Layout.TitleBottomSpacing > 0 ? (int)schema.Layout.TitleBottomSpacing : 6;
            model.Layout.FooterPaddingTop = (int)schema.Layout.FooterPaddingTop;
            model.Layout.FooterPaddingBottom = (int)schema.Layout.FooterPaddingBottom;
            model.Layout.SectionMarginBottom = (int)schema.Layout.SectionMarginBottom;
            model.Layout.SectionBaseScale = schema.Layout.SectionBaseScale;
            model.Layout.HeaderBaseScale = schema.Layout.HeaderBaseScale;
        }

        foreach (var sec in schema.Sections)
        {
            model.Sections.Add(new TemplateSection
            {
                Id = sec.Id,
                Title = sec.Title,
                Type = sec.Type,
                DataSource = sec.DataSource,
                Content = sec.Content,
                IsHalfWidth = sec.IsHalfWidth,
                FontFamily = sec.FontFamily,
                TextColor = sec.TextColor,
                FontSize = sec.FontSize,
                PageBreakBefore = sec.PageBreakBefore,
                MarginBottom = sec.MarginBottom,
                MinHeight = sec.MinHeight
            });
        }

        return model;
    }

    public static TemplateBuilderSchema MapModelToSchema(TemplateBuilderModel model)
    {
        var schema = new TemplateBuilderSchema();

        schema.Theme.PrimaryColor = model.Theme.PrimaryColor;
        schema.Theme.SecondaryColor = model.Theme.SecondaryColor;
        schema.Theme.BodyTextColor = model.Theme.BodyTextColor;
        schema.Theme.TitleFontFamily = model.Theme.TitleFontFamily;
        schema.Theme.FontFamily = model.Theme.FontFamily;
        schema.Theme.BaseFontSize = model.Theme.BaseFontSize;


        schema.Header.LogoUrl = model.Header.LogoUrl;
        schema.Header.LogoHeight = model.Header.LogoHeight;
        schema.Header.ShowLogo = model.Header.ShowLogo;
        schema.Header.FontFamily = model.Header.FontFamily;
        schema.Header.TextColor = model.Header.TextColor;
        schema.Header.HeaderRepeatStyle = model.Header.HeaderRepeatStyle;
        schema.Header.DocumentSubtitle = model.Header.DocumentSubtitle;

        if (model.Header.RepeatingHeader != null)
        {
            schema.Header.RepeatingHeader.IsEnabled = model.Header.RepeatingHeader.IsEnabled;
            schema.Header.RepeatingHeader.ShowLogo = model.Header.RepeatingHeader.ShowLogo;
            schema.Header.RepeatingHeader.LogoHeight = model.Header.RepeatingHeader.LogoHeight;
            schema.Header.RepeatingHeader.BackgroundColor = model.Header.RepeatingHeader.BackgroundColor;
            schema.Header.RepeatingHeader.TextColor = model.Header.RepeatingHeader.TextColor;
            schema.Header.RepeatingHeader.ShowBorderBottom = model.Header.RepeatingHeader.ShowBorderBottom;
            schema.Header.RepeatingHeader.BorderColor = model.Header.RepeatingHeader.BorderColor;
            schema.Header.RepeatingHeader.ShowRevisionInfo = model.Header.RepeatingHeader.ShowRevisionInfo;
        }

        schema.Footer.Column1 = model.Footer.Column1;
        schema.Footer.Column2 = model.Footer.Column2;
        schema.Footer.LegalText = model.Footer.LegalText;
        schema.Footer.FontFamily = model.Footer.FontFamily;
        schema.Footer.TextColor = model.Footer.TextColor;
        schema.Footer.FooterRepeatStyle = model.Footer.FooterRepeatStyle;

        schema.Batch.ShowBatchNote = model.Batch.ShowBatchNote;
        schema.Batch.BatchNoteFormat = model.Batch.BatchNoteFormat;

        if (model.Layout != null)
        {
            schema.Layout.TopMargin = model.Layout.TopMargin;
            schema.Layout.BottomMargin = model.Layout.BottomMargin;
            schema.Layout.LeftMargin = model.Layout.LeftMargin;
            schema.Layout.RightMargin = model.Layout.RightMargin;
            schema.Layout.HeaderPaddingTop = model.Layout.HeaderPaddingTop;
            schema.Layout.HeaderPaddingBottom = model.Layout.HeaderPaddingBottom;
            schema.Layout.HeaderPaddingLeft = model.Layout.HeaderPaddingLeft;
            schema.Layout.HeaderPaddingRight = model.Layout.HeaderPaddingRight;
            schema.Layout.ContentPaddingTop = model.Layout.ContentPaddingTop;
            schema.Layout.BatchNotePaddingBottom = model.Layout.BatchNotePaddingBottom;
            schema.Layout.SectionSpacing = model.Layout.SectionSpacing;
            schema.Layout.TitleBottomSpacing = model.Layout.TitleBottomSpacing;
            schema.Layout.FooterPaddingTop = model.Layout.FooterPaddingTop;
            schema.Layout.FooterPaddingBottom = model.Layout.FooterPaddingBottom;
            schema.Layout.SectionMarginBottom = model.Layout.SectionMarginBottom;
            schema.Layout.SectionBaseScale = model.Layout.SectionBaseScale;
            schema.Layout.HeaderBaseScale = model.Layout.HeaderBaseScale;
        }

        foreach (var sec in model.Sections)
        {
            schema.Sections.Add(new TemplateSectionSchema
            {
                Id = sec.Id,
                Title = sec.Title,
                Type = sec.Type,
                DataSource = sec.DataSource,
                Content = sec.Content,
                IsHalfWidth = sec.IsHalfWidth,
                FontFamily = sec.FontFamily,
                TextColor = sec.TextColor,
                FontSize = sec.FontSize,
                PageBreakBefore = sec.PageBreakBefore,
                MarginBottom = sec.MarginBottom,
                MinHeight = sec.MinHeight
            });
        }

        return schema;
    }
}
