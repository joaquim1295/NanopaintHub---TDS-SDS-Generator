using System;
using System.Collections.Generic;

namespace NanopaintHub.Application.Tds;

public class TemplateBuilderSchema
{
    public TemplateHeaderSchema Header { get; set; } = new();
    public TemplateThemeSchema Theme { get; set; } = new();
    public TemplateFooterSchema Footer { get; set; } = new();
    public TemplateBatchSchema Batch { get; set; } = new();
    public List<TemplateSectionSchema> Sections { get; set; } = new();
}

public class TemplateHeaderSchema
{
    public string InfoDocType { get; set; } = "";
    public string InfoCategory { get; set; } = "";
    public DateTimeOffset? InfoRevisedDate { get; set; }
    public string InfoVersion { get; set; } = "";
    public string IdentType { get; set; } = "";
    public string IdentName { get; set; } = "";
    public string IdentDesc { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public int LogoHeight { get; set; } = 34;
    public bool ShowLogo { get; set; } = true;
    public string FontFamily { get; set; } = "";
    public string TextColor { get; set; } = "";
    public string HeaderRepeatStyle { get; set; } = "None";
}

public class TemplateThemeSchema
{
    public string PrimaryColor { get; set; } = "#0f2a4a";
    public string SecondaryColor { get; set; } = "#19528d";
    public string FontFamily { get; set; } = "Segoe UI";
}

public class TemplateFooterSchema
{
    public string Column1 { get; set; } = "";
    public string Column2 { get; set; } = "";
    public string LegalText { get; set; } = "";
    public string FontFamily { get; set; } = "";
    public string TextColor { get; set; } = "";
    public string FooterRepeatStyle { get; set; } = "None";
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
}
