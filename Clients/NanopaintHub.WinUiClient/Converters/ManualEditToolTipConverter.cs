using System;
using Microsoft.UI.Xaml.Data;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub_WinUIClient.Converters;

public sealed class ManualEditToolTipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is SupplierProductComposition comp)
        {
            // Convert UTC date to local time for display
            var localDate = comp.LastModified.ToLocalTime();
            var dateStr = localDate.ToString("dd/MM/yyyy HH:mm");
            
            if (!string.IsNullOrEmpty(comp.OriginalPercentageText))
            {
                return $"Editado manualmente em {dateStr}. O valor difere do valor originalmente extraído pela SDS (original: {comp.OriginalPercentageText}).";
            }
            return $"Editado manualmente em {dateStr}.";
        }
        return "Editado manualmente";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
