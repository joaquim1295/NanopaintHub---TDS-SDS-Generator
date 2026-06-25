# System Patterns

## Architecture Structure
The application follows a Clean Architecture approach:
- **Core Domain**: `NanopaintHub.Domain` (Entities and repository contracts)
- **Application Logic**: `NanopaintHub.Application` (Contracts, Use Cases, Tds schemas, and HTML generators)
- **Infrastructure**: 
  - `NanopaintHub.Infrastructure.Documents` (Puppeteer PDF rendering, editor service, layout calculation)
  - `NanopaintHub.LocalData` (SQLite, SQLCipher database persistence)
- **UI Client**: `NanopaintHub.WinUIClient` (Desktop Views, ViewModels, and UI model mappings)

## Key Design Patterns
- **MVVM (Model-View-ViewModel)**: Client strictly adheres to MVVM. Views bind to ViewModels via `x:Bind` for commands and properties, with code-behinds restricted to UI events (e.g. file pickers).
- **Dependency Injection / Inversion**: Core application logic depends on abstractions (e.g. `ITdsHtmlGenerator`, `IPdfLayoutService`, `IChromiumTinyMceEditorService`) registered via `AddNanopaintHubDocuments` and injected via constructor arguments.
- **Multi-Engine PDF Strategy (Strategy Pattern)**: 
  - `IDocumentGeneratorService` é uma fachada de roteamento.
  - O `DocumentGeneratorRouter` despacha requisições para `WinUiWebView2PdfGenerator`, `QuestPdfDocumentGeneratorService` ou `ITextDocumentGeneratorService` baseado na injeção de `PdfGeneratorOptions` (`TdsEngine` / `SdsEngine`).
  - Cada motor atende a um propósito: **WebView2** (edição criativa TDS gratuita via HTML), **QuestPDF** (layouts estruturados SDS/matrizes C# fluidas), **iText7** (validação/teste HTML corporativo).
- **Decoupled HTML Templating**: Conversion from the template builder schema to Handlebars-compatible HTML/CSS is isolated in `ITdsHtmlGenerator` within the Application layer, keeping it presentation-neutral and reusable.
- **Out-of-Process Web Automation**: Visual editors (for whole pages or individual sections) launch out-of-process headed Chromium instances using `IChromiumTinyMceEditorService` via Puppeteer Sharp, avoiding unstable local WebView2 dependencies.
- **Safety Margin Clamping**: Safety print margins are normalized during template synchronization using `IPdfLayoutService`, ensuring layout preview dimensions reflect final printed A4 boundaries.

## UI Principles
- **Modern Windows App Aesthetics**: Consistent styling utilizing Expanders, ColorPickers, and NumberBoxes.
- **Responsive Previews**: HTML/CSS previews synchronize normalized margins, providing WYSIWYG correctness.
