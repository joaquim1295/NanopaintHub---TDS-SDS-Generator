using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace NanopaintHub_WinUIClient.Converters;

public sealed partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            var isNegated = parameter?.ToString()?.Equals("negate", StringComparison.OrdinalIgnoreCase) ?? false;
            if (isNegated) b = !b;
            
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
