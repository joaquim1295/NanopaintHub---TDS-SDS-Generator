using CommunityToolkit.Mvvm.ComponentModel;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class KeyValuePairItem : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    public KeyValuePairItem() { }

    public KeyValuePairItem(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
