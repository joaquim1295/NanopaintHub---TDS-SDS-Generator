namespace NanopaintHub.Domain.Entities;

using System;
using NanopaintHub.Domain.Common;
using NanopaintHub.Domain.Enums;

public class TestBatch : Entity
{
    public Guid ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    
    public string TestValues { get; set; } = string.Empty;
    public string CurveGraphPath { get; set; } = string.Empty;
    
    public DateTime ManufactureDate { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.LocalOnly;
    

    public void MarkWithValidationError()
    {
        SyncStatus = SyncStatus.ValidationError;
        MarkAsModified();
    }

    public void MarkAsSynced()
    {
        SyncStatus = SyncStatus.Synced;
        MarkAsModified();
    }

    public void UpdateValues(string newTestValuesJson)
    {
        TestValues = newTestValuesJson;
        
        if (SyncStatus == SyncStatus.Synced)
        {
            SyncStatus = SyncStatus.PendingUpdate;
        }
        
        MarkAsModified();
    }
}

