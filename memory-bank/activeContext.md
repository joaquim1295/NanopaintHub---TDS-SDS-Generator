# Active Context

## Current Focus
- Stabilizing the `TemplateEditorPage.xaml` UI and interactions.
- Ensuring Color Pickers correctly assume colors when the "OK" button is pressed.
- Ensuring Image (Logo) uploads are persisted locally so they don't break HTML/PDF rendering.

## Recent Changes
- Fixed Flyout Popups not closing in WinUI 3: Implemented `VisualTreeHelper.GetOpenPopupsForXamlRoot` to accurately find and close open `ColorPicker` flyouts.
- Fixed Logo URL paths: When picking a logo, it is now copied to `%LocalAppData%\NanopaintHub\Assets\Logos` and the path is stored as `file:///...` for PuppeteerSharp compatibility.
- Transitioned from WinUI 3 `WebView2` (which was rendering a blank page) to an isolated headless Chromium instance via `PuppeteerSharp` for the TinyMCE editing experience.

## Active Decisions
- Use `PuppeteerSharp` as the standard isolation environment for Web-based text editing, instead of relying on the local system's WebView2 runtime which proved unstable on the host machine.
- Use LocalAppData for storing document assets (like Logos) to guarantee they are available at render-time.

## Next Steps
- Finalize any further interactions in the Template Editor (like drag-and-drop or linking Sections to actual data models).
- Prepare to integrate this Builder logic with the actual PDF Generation Engine and Product data.
