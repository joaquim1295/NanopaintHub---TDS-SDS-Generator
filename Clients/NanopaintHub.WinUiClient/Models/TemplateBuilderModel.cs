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

    public ObservableCollection<TemplateSection> Sections { get; } = new();
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
    private string _headerRepeatStyle = "None";
}

public partial class TemplateTheme : ObservableObject
{
    [ObservableProperty]
    private string _primaryColor = "#0f2a4a";

    [ObservableProperty]
    private string _secondaryColor = "#19528d";

    [ObservableProperty]
    private string _fontFamily = "Segoe UI";
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
    private string _footerRepeatStyle = "None";
}

public partial class TemplateSection : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _title = "Nova Secção";

    /// <summary>Text, Table, BulletPoints, Charts, RichText</summary>
    [ObservableProperty]
    private string _type = "Text";

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
}
