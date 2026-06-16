---
name: generate-pdf-puppeteer
description: Regras rigorosas e procedimentos para renderizar os Documentos Técnicos (TDS e SDS) usando HTML desserializado, Handlebars.NET, PuppeteerSharp e PdfSharpCore, mantendo o ambiente blindado contra falhas locais e garantindo compatibilidade com caminhos URI em `file:///`.
---

# SKILL: Geração de PDF via PuppeteerSharp (NanopaintHub)

Esta _skill_ ensina o agente Antigravity a criar ou modificar o pipeline robusto de geração de PDFs sem quebrar o isolamento nativo de Windows.

## 1. O Pipeline das Três Camadas
A geração não usa Office Automation. Usa três camadas de abstração:

1. **Camada de Semântica e Ligação de Dados (Handlebars.NET)**
   - Extraia os blocos JSON do SQLite (`FeaturesJson`, etc.).
   - Use `System.Text.Json.JsonSerializer` para desserializar para estruturas brutas do C# (como `List<string>` e `Dictionary<string, string>`).
   - Nunca injete variáveis manualmente no texto HTML por concatenação de `string`.
   - Use as tags `{{#each}}` e `{{#if}}` do Handlebars.NET para acoplar os dicionários de dados do C# ao HTML do Template.

2. **Camada de Renderização Pura (PuppeteerSharp)**
   - Lance sempre instâncias `Headless = true` do Chromium usando o SDK do Puppeteer.
   - Todo e qualquer ficheiro referenciado no HTML da página gerada (Logos, CSS, JS) **deve** usar caminhos locais absolutos formatados como `file:///C:/Users/.../AppData/Local/...`. Caminhos relativos falham no contexto invisível.
   - Especifique precisamente as margens (TopMargin, BottomMargin, LeftMargin, RightMargin) usando os tipos próprios do Puppeteer para garantir espaçamento para as camadas vetoriais.

3. **Camada de Fusão Gráfica Vetorial (PdfSharpCore)**
   - O Puppeteer cria o "Miolo" em PDF. Em seguida, utilize o PdfSharpCore para injetar PDFs vetoriais com Arte de Cabeçalho e Rodapé (`timbre_tds_cabecalho.pdf` e `timbre_tds_rodape.pdf`).
   - Esta fusão ocorre como pintura em *Background* da matriz de desenho A4 antes de aplicar o texto na camada principal.
