namespace NanopaintHub.WinUiClient.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Generic;
using NanopaintHub.Domain.Entities;

public class SupplierGroup : ObservableCollection<SupplierProduct>
{
    public string Key { get; set; }

    public SupplierGroup(string key, IEnumerable<SupplierProduct> items) : base(items)
    {
        Key = key;
    }
}
