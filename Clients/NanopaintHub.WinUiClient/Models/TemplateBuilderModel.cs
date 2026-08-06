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

    [System.Text.Json.Serialization.JsonObjectCreationHandling(System.Text.Json.Serialization.JsonObjectCreationHandling.Populate)]
    public ObservableCollection<TemplateSection> Sections { get; } = new();
}

public partial class TemplateLayout : ObservableObject
{
    [ObservableProperty]
    private int _topMargin = 24;

    [ObservableProperty]
    private int _bottomMargin = 20;

    [ObservableProperty]
    private int _leftMargin = 20;

    [ObservableProperty]
    private int _rightMargin = 20;

    [ObservableProperty]
    private int _headerPaddingTop = 14;

    [ObservableProperty]
    private int _headerPaddingBottom = 10;

    [ObservableProperty]
    private int _headerPaddingLeft = 20;

    [ObservableProperty]
    private int _headerPaddingRight = 20;

    [ObservableProperty]
    private int _contentPaddingTop = 10;

    [ObservableProperty]
    private int _batchNotePaddingBottom = 12;

    [ObservableProperty]
    private int _sectionSpacing = 6;

    [ObservableProperty]
    private int _titleBottomSpacing = 6;

    [ObservableProperty]
    private int _footerPaddingTop = 8;

    [ObservableProperty]
    private int _footerPaddingBottom = 10;

    [ObservableProperty]
    private int _sectionMarginBottom = 14;

    [ObservableProperty]
    private double _headerBaseScale = 1.0;

    public int HeaderBaseScalePercentage
    {
        get => (int)System.Math.Round(HeaderBaseScale * 100);
        set 
        {
            if (System.Math.Round(HeaderBaseScale * 100) != value)
            {
                HeaderBaseScale = value / 100.0;
                OnPropertyChanged(nameof(HeaderBaseScalePercentage));
            }
        }
    }

    public int SectionBaseScalePercentage
    {
        get => (int)System.Math.Round(SectionBaseScale * 100);
        set 
        {
            if (System.Math.Round(SectionBaseScale * 100) != value)
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
    
    [ObservableProperty]
    private string _documentSubtitle = "Technical Data Sheet";
    
    // New Styling Properties for the default Layout
    [ObservableProperty]
    private string _headerBackgroundColor = "#0f2a4a";
    
    [ObservableProperty]
    private string _titleFontFamily = "Segoe UI";
    
    [ObservableProperty]
    private string _titleColor = "#ffffff";
    
    [ObservableProperty]
    private string _subtitleFontFamily = "Segoe UI";
    
    [ObservableProperty]
    private string _subtitleColor = "#9ac7f4";
    
    [ObservableProperty]
    private string _descriptionColor = "#b7d7f6";

    [ObservableProperty]
    private bool _showProductImage = true;

    [ObservableProperty]
    private int _productImageMaxHeight = 100;

    [ObservableProperty]
    private string _productImageAlignment = "Top";

    [ObservableProperty]
    private int _productImageMarginTop = 0;

    [ObservableProperty]
    private TemplateRepeatingHeader _repeatingHeader = new();
}

public partial class TemplateRepeatingHeader : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _showLogo = true;

    [ObservableProperty]
    private double _logoHeight = 25;

    [ObservableProperty]
    private string _backgroundColor = "";

    [ObservableProperty]
    private string _textColor = "#19528d";

    [ObservableProperty]
    private bool _showBorderBottom = true;

    [ObservableProperty]
    private string _borderColor = "#19528d";

    [ObservableProperty]
    private bool _showRevisionInfo = true;
}

public partial class TemplateTheme : ObservableObject
{
    [ObservableProperty]
    private string _primaryColor = "#0f2a4a";

    [ObservableProperty]
    private string _secondaryColor = "#19528d";

    [ObservableProperty]
    private string _bodyTextColor = "#223145";

    [ObservableProperty]
    private string _titleFontFamily = "Segoe UI";

    [ObservableProperty]
    private string _fontFamily = "Segoe UI";

    [ObservableProperty]
    private double _baseFontSize = 9;
}

public partial class TemplateFooter : ObservableObject
{
    [ObservableProperty]
    private string _column1 = "NanoPaint, Lda.\nCIIES – Sala 23/28\nAvenida de Tibães, 1199\n4770-568 Vale de São Cosme, Portugal";

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

    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private bool _isTdsContext = false;

    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private bool _isSdsContext = false;
}
