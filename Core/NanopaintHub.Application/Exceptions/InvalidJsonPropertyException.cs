namespace NanopaintHub.Application.Exceptions;

/// <summary>JSON de propriedade do produto inválido (array ou object esperado).</summary>
public sealed class InvalidJsonPropertyException : Exception
{
    public string PropertyName { get; }

    public InvalidJsonPropertyException(string propertyName, string message, Exception? inner = null)
        : base(message, inner)
    {
        PropertyName = propertyName;
    }
}
