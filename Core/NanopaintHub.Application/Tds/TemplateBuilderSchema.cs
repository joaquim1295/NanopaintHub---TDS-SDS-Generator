using System;
using System.Collections.Generic;

namespace NanopaintHub.Application.Tds;

public class TemplateBuilderSchema
{
    public TemplateHeaderSchema Header { get; set; } = new();
    public TemplateThemeSchema Theme { get; set; } = new();
    public TemplateFooterSchema Footer { get; set; } = new();
    public TemplateBatchSchema Batch { get; set; } = new();
    public TemplateLayoutSchema Layout { get; set; } = new();
    public List<TemplateSectionSchema> Sections { get; set; } = new();
}

public class TemplateLayoutSchema
{
    public double TopMargin { get; set; } = 24;
    public double BottomMargin { get; set; } = 20;
    public double LeftMargin { get; set; } = 20;
    public double RightMargin { get; set; } = 20;
    public double HeaderPaddingTop { get; set; } = 14;     // in mm
    public double HeaderPaddingBottom { get; set; } = 10;  // in mm
    public double HeaderPaddingLeft { get; set; } = 20;    // in mm
    public double HeaderPaddingRight { get; set; } = 20;   // in mm
    public double ContentPaddingTop { get; set; } = 10;
    public double BatchNotePaddingBottom { get; set; } = 12;
    public double SectionSpacing { get; set; } = 16;
    public double TitleBottomSpacing { get; set; } = 6;
    public double FooterPaddingTop { get; set; } = 8;      // in px
    public double FooterPaddingBottom { get; set; } = 10;  // in px
    public double SectionMarginBottom { get; set; } = 14;  // in px
    public double SectionBaseScale { get; set; } = 1.0;

    [System.Text.Json.Serialization.JsonIgnore]
    public double SectionBaseScalePercentage
    {
        get => System.Math.Round(SectionBaseScale * 100);
        set => SectionBaseScale = value / 100.0;
    }

    public double HeaderBaseScale { get; set; } = 1.0;

    [System.Text.Json.Serialization.JsonIgnore]
    public double HeaderBaseScalePercentage
    {
        get => System.Math.Round(HeaderBaseScale * 100);
        set => HeaderBaseScale = value / 100.0;
    }
}

public class TemplateHeaderSchema
{
    public string LogoUrl { get; set; } = "";
    public int LogoHeight { get; set; } = 34;
    public bool ShowLogo { get; set; } = true;
    public string FontFamily { get; set; } = "";
    public string TextColor { get; set; } = "";
    public string HeaderRepeatStyle { get; set; } = "Original";
    public string DocumentSubtitle { get; set; } = "Technical Data Sheet";
    public double FontSize { get; set; } = -1;
    
    // New Styling Properties for the default Layout
    public string HeaderBackgroundColor { get; set; } = "#0f2a4a"; 
    public string TitleFontFamily { get; set; } = "Segoe UI";
    public string TitleColor { get; set; } = "#ffffff";
    public string SubtitleFontFamily { get; set; } = "Segoe UI";
    public string SubtitleColor { get; set; } = "#9ac7f4";
    public string DescriptionColor { get; set; } = "#b7d7f6";
}

public class TemplateThemeSchema
{
    public string PrimaryColor { get; set; } = "#0f2a4a";
    public string SecondaryColor { get; set; } = "#19528d";
    public string BodyTextColor { get; set; } = "#223145";
    public string TitleFontFamily { get; set; } = "Segoe UI";
    public string FontFamily { get; set; } = "Segoe UI";
    public double BaseFontSize { get; set; } = 9;
}

public class TemplateFooterSchema
{
    public string Column1 { get; set; } = "NanoPaint, Lda.\nCIIES – Sala 23/28\nAvenida de Tibães, 1199\n4770-568 Vale de São Cosme, Portugal";
    public string Column2 { get; set; } = "";
    public string LegalText { get; set; } = "";
    public string FontFamily { get; set; } = "";
    public string TextColor { get; set; } = "";
    public string FooterRepeatStyle { get; set; } = "Original";

}

public class TemplateBatchSchema
{
    public bool ShowBatchNote { get; set; } = true;
    public string BatchNoteFormat { get; set; } = "Batch data applied: {batch} (merged with product specifications where applicable).";
}

public class TemplateSectionSchema
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "Text";
    public string DataSource { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsHalfWidth { get; set; } = false;
    public string FontFamily { get; set; } = "";
    public string TextColor { get; set; } = "";
    public double FontSize { get; set; } = -1;
    public bool PageBreakBefore { get; set; } = false;
    public double MarginBottom { get; set; } = -1;
    public double MinHeight { get; set; } = 0;
    public bool IsDataBound { get; set; } = false;
}
