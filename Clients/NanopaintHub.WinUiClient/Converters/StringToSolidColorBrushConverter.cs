using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace NanopaintHub_WinUIClient.Converters;

public sealed partial class StringToSolidColorBrushConverter : IValueConverter
{
    private readonly StringToColorConverter _colorConverter = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = (Color)_colorConverter.Convert(value, typeof(Color), parameter, language);
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is SolidColorBrush brush)
        {
            return _colorConverter.ConvertBack(brush.Color, typeof(string), parameter, language);
        }
        return "#000000";
    }
}
