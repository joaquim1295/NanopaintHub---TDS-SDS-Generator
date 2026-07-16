using System;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Data;

namespace NanopaintHub_WinUIClient.Converters;

public sealed partial class OperatorExtractorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string percentageText)
        {
            if (string.IsNullOrWhiteSpace(percentageText))
                return string.Empty;

            var match = Regex.Match(percentageText, @"^\s*(>=|<=|>|<)\s*");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
