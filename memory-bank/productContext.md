# Product Context

## Purpose
Nanopaint requires technical documentation (TDS, SDS) for its advanced products (e.g., Piezoresistive inks, Silver inks). The NanopaintHub application solves the problem of standardizing and visually designing these documents without relying on external fragile tools.

## Problems Solved
- **Inconsistent Documentation**: Solved via a structured `TemplateBuilderModel` that enforces Headers, Footers, and standard layout blocks.
- **Difficult Formatting**: Solved by embedding the TinyMCE editor (via a local HTML host and PuppeteerSharp) which provides standard Rich Text tools.
- **Image Linking Issues**: Solved by automatically copying selected Logo files to `%LocalAppData%\NanopaintHub\Assets\Logos` and converting them to `file:///` URIs to guarantee the headless browser can render them.

## Expected User Experience
Users expect a smooth, native-feeling Windows app (WinUI 3). They want to click "Editar", have a fast and robust editor pop up, apply colors intuitively with "OK" buttons, and save their templates effortlessly to be later combined with real Product Data.
