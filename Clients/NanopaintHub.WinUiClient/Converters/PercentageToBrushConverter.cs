using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace NanopaintHub_WinUIClient.Converters;

public class PercentageToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double percentage)
        {
            if (percentage <= 0 || percentage > 100)
            {
                // Returns a red brush for invalid values
                return new SolidColorBrush(Colors.Red);
            }
        }
        
        // Return default (can be null to fallback to original style)
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
