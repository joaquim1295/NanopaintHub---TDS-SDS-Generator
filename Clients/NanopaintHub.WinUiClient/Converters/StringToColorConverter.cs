using System;
using Microsoft.UI.Xaml.Data;
using Windows.UI;

namespace NanopaintHub_WinUIClient.Converters;

public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex && hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
        {
            try
            {
                byte a = 255;
                int r, g, b;
                
                if (hex.Length == 9)
                {
                    a = System.Convert.ToByte(hex.Substring(1, 2), 16);
                    r = System.Convert.ToByte(hex.Substring(3, 2), 16);
                    g = System.Convert.ToByte(hex.Substring(5, 2), 16);
                    b = System.Convert.ToByte(hex.Substring(7, 2), 16);
                }
                else
                {
                    r = System.Convert.ToByte(hex.Substring(1, 2), 16);
                    g = System.Convert.ToByte(hex.Substring(3, 2), 16);
                    b = System.Convert.ToByte(hex.Substring(5, 2), 16);
                }

                return Color.FromArgb(a, (byte)r, (byte)g, (byte)b);
            }
            catch { }
        }
        return Color.FromArgb(255, 0, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        return "#000000";
    }
}
