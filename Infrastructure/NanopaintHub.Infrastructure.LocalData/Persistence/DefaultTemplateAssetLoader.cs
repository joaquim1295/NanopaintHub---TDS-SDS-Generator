namespace NanopaintHub.Infrastructure.LocalData.Persistence;

/// <summary>
/// Carrega HTML default TDS/SDS do output da app WinUI ou de recursos embutidos (fallback).
/// </summary>
internal static class DefaultTemplateAssetLoader
{
    private const string PremiumTdsMarker = "header-banner";

    public static async Task<string> LoadTdsHtmlAsync(CancellationToken cancellationToken = default)
    {
        var html = await LoadAsync("MinimalTds.html", "NanopaintHub.DefaultTemplates.MinimalTds.html", cancellationToken);
        if (!html.Contains(PremiumTdsMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "O template TDS carregado não é a versão premium esperada (header-banner em falta).");
        }

        return html;
    }

    public static Task<string> LoadSdsHtmlAsync(CancellationToken cancellationToken = default) =>
        LoadAsync("MinimalSds.html", "NanopaintHub.DefaultTemplates.MinimalSds.html", cancellationToken);

    private static async Task<string> LoadAsync(
        string fileName,
        string embeddedResourceName,
        CancellationToken cancellationToken)
    {
        foreach (var path in GetCandidatePaths(fileName))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var fromFile = await File.ReadAllTextAsync(path, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fromFile))
            {
                return fromFile;
            }
        }

        var assembly = typeof(DefaultTemplateAssetLoader).Assembly;
        await using var stream = assembly.GetManifestResourceStream(embeddedResourceName);
        if (stream is null)
        {
            throw new FileNotFoundException(
                $"Template default não encontrado: {fileName} (ficheiro nem recurso embutido).");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static IEnumerable<string> GetCandidatePaths(string fileName)
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", "Templates", fileName);

        var assemblyDir = Path.GetDirectoryName(typeof(DefaultTemplateAssetLoader).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(assemblyDir))
        {
            yield return Path.Combine(assemblyDir, "Assets", "Templates", fileName);
        }
    }
}
