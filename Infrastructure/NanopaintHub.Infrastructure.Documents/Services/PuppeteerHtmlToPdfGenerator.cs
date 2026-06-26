using System;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace NanopaintHub.Infrastructure.Documents.Services;

public class PuppeteerHtmlToPdfGenerator : IHtmlToPdfGenerator
{
    public async Task<byte[]> GeneratePdfAsync(string htmlContent, string pageFormat = "A4", bool isLandscape = false)
    {
        var format = pageFormat?.ToLowerInvariant() == "letter" ? PaperFormat.Letter : PaperFormat.A4;

        return await ChromiumBrowserSession.RunAsync(async browser =>
        {
            await using var page = await browser.NewPageAsync();
            
            // Set content and wait for network idle to ensure Paged.js and other scripts execute
            await page.SetContentAsync(htmlContent, new SetContentOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Load }
            });

            // If we are using Paged.js, wait for it to finish rendering the pages
            try
            {
                await page.WaitForSelectorAsync(".pagedjs_pages", new WaitForSelectorOptions { Timeout = 10000 });
                // Small additional delay to ensure Paged.js has completed layout
                await Task.Delay(500);
            }
            catch (WaitTaskTimeoutException)
            {
                // It's possible Paged.js wasn't used or failed, continue with standard PDF generation
                Console.WriteLine("[Warning] Paged.js '.pagedjs_pages' selector not found within 10 seconds.");
            }

            var pdfOptions = new PdfOptions
            {
                Format = format,
                Landscape = isLandscape,
                PrintBackground = true,
                PreferCSSPageSize = true, // Allow Paged.js to control page size via CSS
                MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" },
                DisplayHeaderFooter = false
            };

            return await page.PdfDataAsync(pdfOptions);
        });
    }
}
