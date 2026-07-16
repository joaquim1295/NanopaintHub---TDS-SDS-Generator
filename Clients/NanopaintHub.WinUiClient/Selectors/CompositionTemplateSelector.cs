namespace NanopaintHub_WinUIClient.Selectors;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NanopaintHub.Domain.Entities;

public sealed partial class CompositionTemplateSelector : DataTemplateSelector
{
    public DataTemplate SubstanceTemplate { get; set; } = null!;
    public DataTemplate SupplierProductTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is NanopaintHub_WinUIClient.ViewModels.CompositionViewModel comp)
        {
            if (comp.SupplierProduct != null)
            {
                return SupplierProductTemplate;
            }
            return SubstanceTemplate;
        }

        return base.SelectTemplateCore(item, container);
    }
}
