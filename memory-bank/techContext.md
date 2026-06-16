# Tech Context

## Technologies Used
- **Language**: C# 13.0 (.NET 9.0 SDK installed, Targeting .NET 8.0/9.0)
- **UI Framework**: WinUI 3 (Windows App SDK 1.6+)
- **Architecture**: MVVM with CommunityToolkit.Mvvm
- **Web Automation / PDF**: PuppeteerSharp (v18+)
- **Editor**: TinyMCE (Loaded via local `wysiwyg-host.html` assets)
- **Testing**: xUnit with Moq (NanopaintHub.UnitTests)

## Development Setup
- Project is split across `CORE`, `INFRASTRUCTURE`, `CLIENTS`, `TOOLS`, and `TESTS` directories.
- WinUI 3 project requires specific MSBuild configurations. A typical build is `dotnet build`.
- Local App Data (`%LocalAppData%\NanopaintHub`) is heavily used to cache logos, documents, and local browser binaries.

## Technical Constraints
- **WebView2 Issues**: The standard WinUI 3 WebView2 control rendered as a blank white page in this specific environment. As a workaround, Web UI/Editor components are handled by launching a headless/headed Chromium instance out-of-process via PuppeteerSharp.
- **File URIs**: Since Chromium is launched locally, all images and external assets passed to HTML must use absolute `file:///` URIs.
