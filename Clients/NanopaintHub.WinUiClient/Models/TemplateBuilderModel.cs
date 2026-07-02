using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NanopaintHub_WinUIClient.Models;

public partial class TemplateBuilderModel : ObservableObject
{
    [ObservableProperty]
    private TemplateHeader _header = new();

    [ObservableProperty]
    private TemplateTheme _theme = new();

    [ObservableProperty]
    private TemplateFooter _footer = new();

    [ObservableProperty]
    private TemplateBatch _batch = new();

    [ObservableProperty]
    private TemplateLayout _layout = new();

    public ObservableCollection<TemplateSection> Sections { get; } = new();
}

public partial class TemplateLayout : ObservableObject
{
    [ObservableProperty]
    private double _topMargin = 24;

    [ObservableProperty]
    private double _bottomMargin = 20;

    [ObservableProperty]
    private double _leftMargin = 20;

    [ObservableProperty]
    private double _rightMargin = 20;

    [ObservableProperty]
    private double _headerPaddingTop = 14;

    [ObservableProperty]
    private double _headerPaddingBottom = 10;

    [ObservableProperty]
    private double _contentPaddingTop = 10;

    [ObservableProperty]
    private double _batchNotePaddingBottom = 12;

    [ObservableProperty]
    private double _sectionSpacing = 16;

    [ObservableProperty]
    private double _footerPaddingTop = 8;

    [ObservableProperty]
    private double _footerPaddingBottom = 10;

    [ObservableProperty]
    private double _sectionMarginBottom = 14;

    [ObservableProperty]
    private double _headerBaseScale = 1.0;

    public double HeaderBaseScalePercentage
    {
        get => System.Math.Round(HeaderBaseScale * 100);
        set 
        {
            if (HeaderBaseScale != value / 100.0)
            {
                HeaderBaseScale = value / 100.0;
                OnPropertyChanged(nameof(HeaderBaseScalePercentage));
            }
        }
    }

    public double SectionBaseScalePercentage
    {
        get => System.Math.Round(SectionBaseScale * 100);
        set 
        {
            if (SectionBaseScale != value / 100.0)
            {
                SectionBaseScale = value / 100.0;
                OnPropertyChanged(nameof(SectionBaseScalePercentage));
            }
        }
    }

    [ObservableProperty]
    private double _sectionBaseScale = 1.0;
}

public partial class TemplateHeader : ObservableObject
{
    [ObservableProperty]
    private string _infoDocType = "";

    [ObservableProperty]
    private string _infoCategory = "";

    [ObservableProperty]
    private DateTimeOffset? _infoRevisedDate;

    [ObservableProperty]
    private string _infoVersion = "";

    [ObservableProperty]
    private string _identType = "";

    [ObservableProperty]
    private string _identName = "";

    [ObservableProperty]
    private string _identDesc = "";

    [ObservableProperty]
    private string _logoUrl = "";

    [ObservableProperty]
    private int _logoHeight = 34;

    [ObservableProperty]
    private bool _showLogo = true;

    [ObservableProperty]
    private string _fontFamily = "";

    [ObservableProperty]
    private string _textColor = "";

    [ObservableProperty]
    private string _headerRepeatStyle = "Original";
}

public partial class TemplateTheme : ObservableObject
{
    [ObservableProperty]
    private string _primaryColor = "#0f2a4a";

    [ObservableProperty]
    private string _secondaryColor = "#19528d";

    [ObservableProperty]
    private string _fontFamily = "Segoe UI";

    [ObservableProperty]
    private double _baseFontSize = 9;
}

public partial class TemplateFooter : ObservableObject
{
    [ObservableProperty]
    private string _column1 = "NanoPaint, Lda.\nUniversidade do Minho\nBraga, Portugal";

    [ObservableProperty]
    private string _column2 = "www.nanopaint-tech.com\ninfo@nanopaint-tech.com";

    [ObservableProperty]
    private string _legalText = "Safety Data Sheets (SDS) are available by emailing us...";

    [ObservableProperty]
    private string _fontFamily = "";

    [ObservableProperty]
    private string _textColor = "";

    [ObservableProperty]
    private string _footerRepeatStyle = "Original";
}

public partial class TemplateBatch : ObservableObject
{
    [ObservableProperty]
    private bool _showBatchNote = true;

    [ObservableProperty]
    private string _batchNoteFormat = "Batch data applied: {batch} (merged with product specifications where applicable).";
}

public partial class TemplateSection : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _title = "Nova Secção";

    /// <summary>Text, Table, BulletPoints, Charts, RichText</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditContent))]
    private string _type = "Text";

    public bool CanEditContent => !IsDataBound && (Type == "RichText" || Type == "SdsSectionText" || (Type != null && Type.StartsWith("SdsSection")));

    /// <summary>Qual campo do Produto este bloco lê (ex: LiquidProperties, Features, Charts)</summary>
    [ObservableProperty]
    private string _dataSource = "";

    /// <summary>Para RichText, o conteúdo HTML editado no TinyMCE</summary>
    [ObservableProperty]
    private string _content = "";

    /// <summary>Se True, o bloco tenta ocupar apenas metade do espaço, emparelhando com o próximo</summary>
    [ObservableProperty]
    private bool _isHalfWidth = false;

    [ObservableProperty]
    private string _fontFamily = "";

    [ObservableProperty]
    private string _textColor = "";

    [ObservableProperty]
    private double _fontSize = -1;

    [ObservableProperty]
    private bool _pageBreakBefore;

    [ObservableProperty]
    private double _marginBottom = -1;

    [ObservableProperty]
    private double _minHeight = 0;

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditContent))]
    private bool _isDataBound = false;
}
