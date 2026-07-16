using System;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NanopaintHub.Domain.Common;

public abstract class Entity : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  public Guid Id { get; protected set; } = Guid.NewGuid();

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime LastModified { get; set; } = DateTime.UtcNow;
  
  public void MarkAsModified()
  {
    LastModified = DateTime.UtcNow;
  }
  
}