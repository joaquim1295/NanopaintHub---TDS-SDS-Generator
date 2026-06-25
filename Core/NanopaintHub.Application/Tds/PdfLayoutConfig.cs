namespace NanopaintHub.Application.Tds;

public class PdfLayoutConfig
{
    public bool DisplayHeaderFooter { get; set; }
    public string HeaderTemplate { get; set; } = string.Empty;
    public string FooterTemplate { get; set; } = string.Empty;
    public double LeftMargin { get; set; } = 20;
    public double RightMargin { get; set; } = 20;
    public double TopMargin { get; set; } = 24;
    public double BottomMargin { get; set; } = 20;
    public bool OmitPuppeteerMargins { get; set; }
}
