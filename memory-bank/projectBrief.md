# Project Brief: NanopaintHub

## Overview
NanopaintHub is a modern Windows desktop application (WinUI 3 / .NET 9.0) designed to manage technical documents and products for the "Nanopaint" ecosystem. Its primary goal is to provide a robust, visual template builder for technical documents (like TDS - Technical Data Sheets, and SDS - Safety Data Sheets) and export them to PDF.

## Core Requirements
- **Template Editor**: Must allow visual construction of documents, including global headers/footers, color themes, and individual sections (Rich Text, Bullet Points, Tables).
- **PDF Generation**: Must generate pixel-perfect PDFs from the built HTML/CSS structure.
- **Robust Rich Text Editing**: Requires a robust WYSIWYG editor (TinyMCE) running reliably without breaking the host application.
- **Local Assets Management**: Must handle local files (like logos or images) securely within the AppData folders to ensure rendering works flawlessly.

## Key Goals
- **Wow-factor UI**: The interface should be modern, using WinUI 3 styling, Expander cards, proper padding, and intuitive user controls (Color Pickers, DropDownButtons).
- **Stability**: Given issues with the native WebView2 component in this specific context, the architecture explicitly relies on `PuppeteerSharp` (Headless Chromium) as a stable host for the TinyMCE editor and PDF generation engine.
