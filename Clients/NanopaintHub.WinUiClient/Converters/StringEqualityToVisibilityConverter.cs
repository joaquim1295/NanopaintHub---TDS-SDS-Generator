using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace NanopaintHub_WinUIClient.Converters;

public sealed partial class StringEqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string valStr && parameter is string paramStr)
        {
            return valStr.Equals(paramStr, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
