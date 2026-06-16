namespace NanopaintHub.Domain.Entities;

using System;
using NanopaintHub.Domain.Common;

public class DocumentTemplate : Entity
{
    public string TemplateName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    
    // Caminho para o PDF de fundo do Marketing
    public string LetterheadPdfPath { get; set; } = string.Empty;
    
    public int TopMargin  { get; set; }
    public int BottomMargin { get; set; }

    public string HtmlStructure { get; set; } = string.Empty;
    public string CssStructure { get; set; } = string.Empty;
    public string BuilderJson { get; set; } = string.Empty;


}