namespace NanopaintHub.Domain.Entities;

using System;

/// <summary>
/// Tabela Pivot N:M para relacionar uma substância ECHA com múltiplas frases CLP (H e P).
/// </summary>
public class SubstanceHazard
{
    public Guid SubstanceId { get; set; }
    public EchaSubstance? Substance { get; set; }

    public string PhraseCode { get; set; } = string.Empty;
    public ClpPhrase? Phrase { get; set; }
}
