namespace NanopaintHub.Application.Tds;

public sealed record TemplatePlaceholderDefinition(
    string Token,
    string Label,
    string Category,
    string DocumentType,
    string ValueType,
    string Example);

public static class TemplatePlaceholderCatalog
{
    public static IReadOnlyList<TemplatePlaceholderDefinition> All { get; } =
    [
        new("Name", "Nome do produto", "Geral", "ALL", "text", "{{Name}}"),
        new("Code", "Código do produto", "Geral", "ALL", "text", "{{Code}}"),
        new("Description", "Descrição", "Geral", "ALL", "multiline", "{{Description}}"),
        new("RevisionDate", "Data de revisão", "Geral", "ALL", "date", "{{RevisionDate}}"),
        new("Version", "Versão do documento", "Geral", "ALL", "number", "{{Version}}"),
        new("BatchNumber", "Número do lote", "TDS", "TDS", "text", "{{BatchNumber}}"),
        new("Sensor.SensitivityVolts", "Sensibilidade (V)", "Sensor", "TDS", "number", "{{Sensor.SensitivityVolts}}"),
        new("Sensor.SensitivityPerUnit", "Sensibilidade por unidade", "Sensor", "TDS", "number", "{{Sensor.SensitivityPerUnit}}"),
        new("Sensor.ResponseTimeSec", "Tempo de resposta", "Sensor", "TDS", "number", "{{Sensor.ResponseTimeSec}}"),
        new("Sensor.RecoveryTimeSec", "Tempo de recuperação", "Sensor", "TDS", "number", "{{Sensor.RecoveryTimeSec}}"),
        new("Sensor.RangeMin", "Range mínimo", "Sensor", "TDS", "number", "{{Sensor.RangeMin}}"),
        new("Sensor.RangeMax", "Range máximo", "Sensor", "TDS", "number", "{{Sensor.RangeMax}}"),
        new("Charts.LinearitySvg", "Gráfico linearidade", "Gráficos", "TDS", "svg", "{{{Charts.LinearitySvg}}}"),
        new("Charts.ResistanceSvg", "Gráfico resistência", "Gráficos", "TDS", "svg", "{{{Charts.ResistanceSvg}}}"),
        new("Charts.HysteresisSvg", "Gráfico histerese", "Gráficos", "TDS", "svg", "{{{Charts.HysteresisSvg}}}"),
        new("Assets.Logo", "Imagem logo", "Assets", "ALL", "image", "{{Assets.Logo}}"),
        new("Assets.ProductImage", "Imagem produto", "Assets", "ALL", "image", "{{Assets.ProductImage}}"),
        new("Placeholders.CustomText", "Placeholder custom", "Custom", "ALL", "text", "{{Placeholders.CustomText}}"),
        new("Components", "Componentes SDS", "SDS", "SDS", "collection", "{{#each Components}}...{{/each}}"),
    ];

    public static IReadOnlyList<TemplatePlaceholderDefinition> ForDocumentType(string documentType) =>
        All.Where(x => x.DocumentType == "ALL" || string.Equals(x.DocumentType, documentType, StringComparison.OrdinalIgnoreCase))
           .ToList();
}
