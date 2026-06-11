using System;

namespace NanopaintHub.Domain.Common;

public abstract class Entity
{
  public Guid Id { get; protected set; } = Guid.NewGuid();

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime LastModified { get; set; } = DateTime.UtcNow;
  
  public void MarkAsModified()
  {
    LastModified = DateTime.UtcNow;
  }
  
}