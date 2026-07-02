namespace NanopaintHub.Domain.Entities;

using System;
using NanopaintHub.Domain.Common;

public class DocumentRelease : Entity
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // e.g. "SDS", "TDS"
    public string Version { get; set; } = string.Empty; // e.g. "1.0", "1.1"
    public DateTime GeneratedAt { get; set; }
    
    // Caminho local onde o PDF foi guardado originalmente
    public string FilePath { get; set; } = string.Empty;
    
    // O snapshot JSON que permite regenerar a Ficha de Segurança exatamente como era
    // mesmo que a Entity Product ou DocumentTemplate mude no futuro (Time-Travel).
    public string SnapshotJson { get; set; } = string.Empty;
}
