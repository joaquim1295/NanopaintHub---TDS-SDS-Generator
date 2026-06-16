# System Patterns

## Architecture Structure
The application follows a Clean Architecture approach:
- **Core Domain**: `NanopaintHub.Domain`
- **Application Logic**: `NanopaintHub.Application`
- **Infrastructure**: `NanopaintHub.Infrastructure.Documents` (Puppeteer/PDF generation), `NanopaintHub.Infrastructure.LocalData` (Storage)
- **UI Client**: `NanopaintHub.WinUIClient` (WinUI 3 App)

## Key Design Patterns
- **MVVM (Model-View-ViewModel)**: The WinUI 3 client strictly uses MVVM. `TemplateEditorPage.xaml` uses `TemplateEditorViewModel` with `x:Bind` for Two-Way data binding.
- **Dependency Injection**: Services (like `IChromiumTinyMceEditorService`) are registered in `ServiceCollectionExtensions` and injected where needed.
- **Out-of-Process Browser Automation**: Instead of embedding a complex WebView, the `ChromiumTinyMceEditorService` launches a hidden Chromium window via PuppeteerSharp. It injects Javascript to initialize TinyMCE, waits for user input, and extracts the resulting HTML.

## UI Principles
- **Modern Windows App Aesthetics**: Extensive use of Expanders, DropDownButtons, and ColorPickers for a rich editing experience.
- **Explicit User Actions**: Use explicit "OK" or "Aplicar" buttons on Flyouts to ensure the user feels in control of applying changes.
