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

        model.Theme.PrimaryColor = schema.Theme.PrimaryColor;
        model.Theme.SecondaryColor = schema.Theme.SecondaryColor;
        model.Theme.FontFamily = schema.Theme.FontFamily;
        model.Theme.BaseFontSize = schema.Theme.BaseFontSize;

        model.Header.InfoDocType = schema.Header.InfoDocType;
        model.Header.InfoCategory = schema.Header.InfoCategory;
        model.Header.InfoRevisedDate = schema.Header.InfoRevisedDate;
        model.Header.InfoVersion = schema.Header.InfoVersion;
        model.Header.IdentType = schema.Header.IdentType;
        model.Header.IdentName = schema.Header.IdentName;
        model.Header.IdentDesc = schema.Header.IdentDesc;
        model.Header.LogoUrl = schema.Header.LogoUrl;
        model.Header.LogoHeight = schema.Header.LogoHeight;
        model.Header.ShowLogo = schema.Header.ShowLogo;
        model.Header.FontFamily = schema.Header.FontFamily;
        model.Header.TextColor = schema.Header.TextColor;
        model.Header.HeaderRepeatStyle = schema.Header.HeaderRepeatStyle;

        model.Footer.Column1 = schema.Footer.Column1;
        model.Footer.Column2 = schema.Footer.Column2;
        model.Footer.LegalText = schema.Footer.LegalText;
        model.Footer.FontFamily = schema.Footer.FontFamily;
        model.Footer.TextColor = schema.Footer.TextColor;
        model.Footer.FooterRepeatStyle = schema.Footer.FooterRepeatStyle;

        model.Batch.ShowBatchNote = schema.Batch.ShowBatchNote;
        model.Batch.BatchNoteFormat = schema.Batch.BatchNoteFormat;

        if (schema.Layout != null)
        {
            model.Layout.TopMargin = schema.Layout.TopMargin;
            model.Layout.BottomMargin = schema.Layout.BottomMargin;
            model.Layout.LeftMargin = schema.Layout.LeftMargin;
            model.Layout.RightMargin = schema.Layout.RightMargin;
            model.Layout.HeaderPaddingTop = schema.Layout.HeaderPaddingTop;
            model.Layout.HeaderPaddingBottom = schema.Layout.HeaderPaddingBottom;
            model.Layout.ContentPaddingTop = schema.Layout.ContentPaddingTop;
            model.Layout.BatchNotePaddingBottom = schema.Layout.BatchNotePaddingBottom;
            model.Layout.SectionSpacing = schema.Layout.SectionSpacing;
            model.Layout.FooterPaddingTop = schema.Layout.FooterPaddingTop;
            model.Layout.FooterPaddingBottom = schema.Layout.FooterPaddingBottom;
            model.Layout.SectionMarginBottom = schema.Layout.SectionMarginBottom;
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
        schema.Theme.FontFamily = model.Theme.FontFamily;
        schema.Theme.BaseFontSize = model.Theme.BaseFontSize;

        schema.Header.InfoDocType = model.Header.InfoDocType;
        schema.Header.InfoCategory = model.Header.InfoCategory;
        schema.Header.InfoRevisedDate = model.Header.InfoRevisedDate;
        schema.Header.InfoVersion = model.Header.InfoVersion;
        schema.Header.IdentType = model.Header.IdentType;
        schema.Header.IdentName = model.Header.IdentName;
        schema.Header.IdentDesc = model.Header.IdentDesc;
        schema.Header.LogoUrl = model.Header.LogoUrl;
        schema.Header.LogoHeight = model.Header.LogoHeight;
        schema.Header.ShowLogo = model.Header.ShowLogo;
        schema.Header.FontFamily = model.Header.FontFamily;
        schema.Header.TextColor = model.Header.TextColor;
        schema.Header.HeaderRepeatStyle = model.Header.HeaderRepeatStyle;

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
            schema.Layout.ContentPaddingTop = model.Layout.ContentPaddingTop;
            schema.Layout.BatchNotePaddingBottom = model.Layout.BatchNotePaddingBottom;
            schema.Layout.SectionSpacing = model.Layout.SectionSpacing;
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
