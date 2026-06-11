namespace NanopaintHub.Application.Exceptions;

/// <summary>
/// Lançada quando uma entidade necessária para o caso de uso não existe na base de dados.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    public string EntityLabel { get; }
    public object? Key { get; }

    public EntityNotFoundException(string entityLabel, object? key, string message)
        : base(message)
    {
        EntityLabel = entityLabel;
        Key = key;
    }
}
