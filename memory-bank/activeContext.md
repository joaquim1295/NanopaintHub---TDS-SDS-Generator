# Active Context

## Current Focus
- Implementação da **Arquitetura Multi-Engine PDF (Strategy Pattern)** para permitir a geração híbrida de documentos usando WebView2, QuestPDF e iText7.
- Desacoplamento da geração de HTML da geração final de PDF.
- Limpeza e otimização da interface de usuário (`GeneratePdfViewModel`) para consumir a nova arquitetura sem vazamento de código HTML obsoleto.

## Recent Changes
- Decoupled template HTML/CSS generation from the Client layer: created `ITdsHtmlGenerator` / `TdsHtmlGenerator` in the `NanopaintHub.Application` layer and registered it in DI.
- Mapped client UI models to application-level DTOs using `TemplateHtmlParser.MapModelToSchema(...)`.
- Bound Editor actions (Global HTML editing and section-specific content editing) to MVVM commands in `TemplateEditorViewModel` and deleted event handlers from `TemplateEditorPage.xaml.cs`.
- Restored visual section content editing capabilities in the visual builder template ListView by adding an "Editar Conteúdo" button bound to the new command.
- Integrated `IPdfLayoutService` margin normalization in the template edit ViewModel to resolve preview vs print margin discrepancies.
- Deleted obsolete WebView2-based editor files (`TinyMceEditorWindow.xaml` and `.xaml.cs`).
- Expanded test suite to 24 tests covering layout normalization and template generation, and verified successful compilation and execution.

## Active Decisions
- Adotar o **Strategy Pattern** via `DocumentGeneratorRouter` para decidir o motor de PDF dinamicamente (`TdsEngine` e `SdsEngine`) via opções de injeção de dependência.
- Três motores suportados: WebView2 (Puppeteer via HTML), QuestPDF (Nativo C# para SDS) e iText7 (Módulo pdfHTML, opcional para testes).
- Apenas a primeira página das TDS (via WebView2) usa `@page :first { margin-top: 0; }` para cabeçalhos customizados, resolvendo paginação sem custo financeiro (licenças comerciais).

## Next Steps
- Fase 1: Criação das *Agent Skills* para QuestPDF, iText7 e Strategy Router (Concluído).
- Fase 2: Fatiar a interface `IDocumentGeneratorService` e criar `IHtmlEngineService`.
- Fase 3: Desacoplar a lógica de negócio do serviço legado do WebView2 para um `TdsDataGatherer` limpo.
- Fase 4: Implementar o motor `iText7` e completar o motor `QuestPDF`.
