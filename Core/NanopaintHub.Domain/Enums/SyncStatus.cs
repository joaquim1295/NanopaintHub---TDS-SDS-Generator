namespace NanopaintHub.Domain.Enums;

public enum SyncStatus
{
    LocalOnly = 0,        
    PendingInsert = 1,    
    PendingUpdate = 2,    
    Synced = 3,           
    ValidationError = 4   
}
