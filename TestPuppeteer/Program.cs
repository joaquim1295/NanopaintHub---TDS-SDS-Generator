using System;
using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace TestPuppeteer {
    class Program {
        static async Task Main() {
            try {
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                var page = await browser.NewPageAsync();
                
                var html = @"<head>
<style>
body { background: #ff0000; color: #fff; font-size: 50px; font-weight: bold; }
</style>
</head>
<div class=""page"">HELLO WORLD! IF RED BACKGROUND, CSS WORKS!</div>";
                
                await page.SetContentAsync(html);
                var data = await page.PdfDataAsync(new PdfOptions { PrintBackground = true });
                File.WriteAllBytes("test.pdf", data);
                Console.WriteLine("OK! Size: " + data.Length);
                await browser.CloseAsync();
            } catch(Exception ex) {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
