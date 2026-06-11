using System.Text.Json;
using NanopaintHub.Application.Exceptions;

namespace NanopaintHub.Application.Common;

public static class JsonValidation
{
    public static void EnsureJsonArray(string json, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidJsonPropertyException(propertyName, "Esperado um array JSON (ex.: [\"a\",\"b\"]).");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidJsonPropertyException(propertyName, "JSON inválido.", ex);
        }
    }

    public static void EnsureJsonObject(string json, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidJsonPropertyException(propertyName, "Esperado um object JSON (ex.: {\"chave\":\"valor\"}).");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidJsonPropertyException(propertyName, "JSON inválido.", ex);
        }
    }
}
