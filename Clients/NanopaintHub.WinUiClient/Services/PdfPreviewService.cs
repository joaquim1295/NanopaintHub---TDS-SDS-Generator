using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using PdfiumViewer;

namespace NanopaintHub_WinUIClient.Services;

public class PdfPreviewService
{
    /// <summary>
    /// Renders all pages of a PDF to PNG images and returns the temporary file paths.
    /// </summary>
    public async Task<IReadOnlyList<string>> RenderPdfToImagesAsync(string pdfPath)
    {
        if (!File.Exists(pdfPath))
            throw new FileNotFoundException("PDF not found", pdfPath);

        var imagePaths = new List<string>();
        using var document = PdfDocument.Load(pdfPath);
        var dpi = 144;
        for (int i = 0; i < document.PageCount; i++)
        {
            var size = document.PageSizes[i];
            int width = (int)(size.Width * dpi / 72);
            int height = (int)(size.Height * dpi / 72);
            using var bitmap = document.Render(i, width, height, dpi, dpi, PdfRenderFlags.Annotations);
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
            await Task.Run(() => bitmap.Save(tempPath, ImageFormat.Png));
            imagePaths.Add(tempPath);
        }
        return imagePaths;
    }
}
