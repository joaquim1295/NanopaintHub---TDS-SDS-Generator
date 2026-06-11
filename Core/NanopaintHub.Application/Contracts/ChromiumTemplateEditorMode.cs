namespace NanopaintHub.Application.Contracts;

/// <summary>Modo do editor Chromium para templates HTML.</summary>
public enum ChromiumTemplateEditorMode
{
    /// <summary>TinyMCE WYSIWYG — documento completo (estilo Word).</summary>
    WordLike = 0,

    /// <summary>Quill — apenas fragmentos de texto rico (legado/diagnóstico).</summary>
    QuillSimple = 1,
}
