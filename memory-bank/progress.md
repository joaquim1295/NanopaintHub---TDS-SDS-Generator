# Progress

## What Works
- **Clean Architecture & SOLID Scaffolding**: Main application layout, dependencies, and MVVM bindings are aligned.
- **Decoupled Template Generation**: HTML and CSS template construction is encapsulated in the Application Layer service (`ITdsHtmlGenerator`).
- **MVVM Command Routing**: View/ViewModel separation is enforced, views delegate visual editing entirely to VM commands (`EditGlobalHtmlCommand` and `EditSectionContentCommand`).
- **Section Editing**: Section content editing inside the builder is fully functional via out-of-process TinyMCE headed browser automation.
- **Layout Margins Normalization**: PDF safety margin clamping is synchronized between the visual editor preview and backend generator.
- **Obsolete Code Removed**: Embedded WebView2 editor windows have been deleted, clean compilation has been restored.
- **Unit Testing**: 24 tests covering database persistence, layout normalization, and HTML templates pass successfully.

## What's Left to Build
- Connect visual template schemas to dynamic product specs and test batch tables in SQLite.
- Complete visual editor page templates list search or custom categorization filters.

## Current Status
- Client layer refactoring is completed. Visual builder features are aligned with the technical architecture documents.

## Known Issues
- WebView2 hosting remains disabled due to runtime instabilities on the host machine; out-of-process Chromium automation is the primary stable alternative.
