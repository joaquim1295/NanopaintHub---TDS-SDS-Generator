using CommunityToolkit.Mvvm.ComponentModel;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class StringItem : ObservableObject
{
    [ObservableProperty]
    private string _value = string.Empty;

    public StringItem() { }

    public StringItem(string value)
    {
        Value = value;
    }
}
