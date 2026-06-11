# TDS — especificações de sensor e gráficos

## Modelo de dados (híbrido)

| Origem | Conteúdo |
|--------|----------|
| **Produto** (`TdsSensorSpecsJson`) | Valores base: sensibilidade, tempos, range, curvas |
| **Lote** (`TestBatch.TestValues`) | Overrides opcionais (`specOverrides`, `chartOverrides`) |

Na geração do PDF TDS, o sistema faz **merge produto + lote** (lote mais recente se não for seleccionado outro).

## Campos escalares (produto)

- Sensibilidade: V e por unidade (N ou Pa)
- Tempo de resposta / recuperação (s)
- Repetibilidade (%) — opcional
- Resistência em repouso (Ω)
- Temperatura de operação (°C; °F calculado no PDF)
- Range (N ou Pa)
- Histerese (%)

**Drift** (1m, 10m, 30m, 1h — máx. 3 intervalos): previsto no schema; UI e gráfico logarítmico em fase futura.

## Curvas (JSON em «Curvas» no produto)

```json
{
  "linearity": [{"x": 0, "y": 0}, {"x": 500, "y": 498}],
  "resistanceVsForce": [{"x": 0, "y": 1200}, {"x": 500, "y": 600}],
  "hysteresis": {
    "percent": 1.2,
    "loading": [{"x": 0, "y": 0}, {"x": 500, "y": 500}],
    "unloading": [{"x": 500, "y": 495}, {"x": 0, "y": 0}]
  }
}
```

## Override de lote (`TestValues`)

```json
{
  "batchNumber": "LT-2025-001",
  "specOverrides": { "repeatabilityPercent": 0.35 },
  "chartOverrides": {
    "linearity": [{"x": 0, "y": 0}, {"x": 500, "y": 499.2}]
  }
}
```

## PDF

Gráficos são **SVG inline** gerados em C# (`TdsChartSvgRenderer`) e inseridos no template Handlebars (`{{{Charts.LinearitySvg}}}`, etc.). Template de referência: `Clients/NanopaintHub.WinUiClient/Assets/Templates/MinimalTds.html`.
