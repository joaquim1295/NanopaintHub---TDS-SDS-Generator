using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using NanopaintHub.Application.Contracts;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace NanopaintHub_WinUIClient.Services;

public class WinUiWebView2PdfGenerator : IHtmlToPdfGenerator
{
    public async Task<byte[]> GeneratePdfAsync(string html, string pageFormat, bool isLandscape)
    {
        var tcs = new TaskCompletionSource<byte[]>();

        // Ensure we execute on the UI Thread since WebView2 is a UI control
        Console.WriteLine("[DEBUG] WinUiWebView2PdfGenerator: Started GeneratePdfAsync");
        var app = (App)Microsoft.UI.Xaml.Application.Current;
        var dispatcher = app.MainWindow?.DispatcherQueue;
        
        if (dispatcher == null)
        {
            tcs.SetException(new Exception("UI DispatcherQueue not found."));
            return await tcs.Task;
        }

        dispatcher.TryEnqueue(async () =>
        {
            Console.WriteLine("[DEBUG] WinUiWebView2PdfGenerator: Inside TryEnqueue");
            try
            {
                // Create an invisible WebView2 control
                var webView = new WebView2
                {
                    Width = 800,
                    Height = 600,
                    Opacity = 0, // Invisible
                    IsHitTestVisible = false
                };

                // Add to visual tree to allow initialization
                var rootGrid = (Grid)app.MainWindow.Content;
                rootGrid.Children.Add(webView);

                // Initialize CoreWebView2
                Console.WriteLine("[DEBUG] WinUiWebView2PdfGenerator: Awaiting EnsureCoreWebView2Async");
                await webView.EnsureCoreWebView2Async();
                Console.WriteLine("[DEBUG] WinUiWebView2PdfGenerator: EnsureCoreWebView2Async completed");

                // Listen for NavigationCompleted to know when it's fully loaded and rendered
                webView.NavigationCompleted += async (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        try
                        {
                            // Wait for Paged.js physical fragmentation to complete
                            // We poll for the '.pagedjs_pages' element
                            await Task.Delay(500); // Give it a moment to boot scripts
                            
                            for(int i = 0; i < 20; i++) 
                            {
                                var check = await webView.ExecuteScriptAsync("document.querySelectorAll('.pagedjs_pages').length > 0");
                                if (check == "true") break;
                                await Task.Delay(500);
                            }

                            // Generate PDF
                            var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
                            printSettings.ShouldPrintBackgrounds = true;
                            printSettings.ShouldPrintHeaderAndFooter = false;
                            printSettings.Orientation = isLandscape ? CoreWebView2PrintOrientation.Landscape : CoreWebView2PrintOrientation.Portrait;
                            
                            // Adjust margins for Paged.js to take over completely
                            printSettings.MarginBottom = 0;
                            printSettings.MarginTop = 0;
                            printSettings.MarginLeft = 0;
                            printSettings.MarginRight = 0;
                            
                            if (pageFormat.Equals("letter", StringComparison.OrdinalIgnoreCase))
                            {
                                printSettings.PageWidth = 8.5;
                                printSettings.PageHeight = 11;
                            }
                            else
                            {
                                // A4 size in inches
                                printSettings.PageWidth = 8.27;
                                printSettings.PageHeight = 11.69;
                            }

                            try 
                            {
                                using var pdfStream = await webView.CoreWebView2.PrintToPdfStreamAsync(printSettings);
                                if (pdfStream != null)
                                {
                                    using var reader = new DataReader(pdfStream.GetInputStreamAt(0));
                                    var bytes = new byte[pdfStream.Size];
                                    await reader.LoadAsync((uint)pdfStream.Size);
                                    reader.ReadBytes(bytes);
                                    tcs.SetResult(bytes);
                                }
                                else
                                {
                                    tcs.SetException(new Exception("PrintToPdfStreamAsync returned null."));
                                }
                            }
                            catch(Exception pdfEx)
                            {
                                tcs.SetException(pdfEx);
                            }
                            finally
                            {
                                // Clean up
                                rootGrid.Children.Remove(webView);
                            }
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                            rootGrid.Children.Remove(webView);
                        }
                    }
                    else
                    {
                        tcs.SetException(new Exception("WebView2 Navigation failed: " + e.WebErrorStatus));
                        rootGrid.Children.Remove(webView);
                    }
                };

                // Load the HTML string
                Console.WriteLine("[DEBUG] WinUiWebView2PdfGenerator: Navigating to string");
                webView.NavigateToString(html);
                Console.WriteLine("[DEBUG] WinUiWebView2PdfGenerator: Navigation invoked");
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }
}
