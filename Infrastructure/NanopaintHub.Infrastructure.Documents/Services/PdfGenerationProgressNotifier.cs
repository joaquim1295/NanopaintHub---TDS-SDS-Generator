namespace NanopaintHub.Infrastructure.Documents.Services;

using NanopaintHub.Application.Contracts;

public sealed class PdfGenerationProgressNotifier : IPdfGenerationProgressNotifier
{
    public event EventHandler<PdfGenerationProgressEventArgs>? ProgressChanged;

    public void Report(string message, double? percent = null)
    {
        ProgressChanged?.Invoke(this, new PdfGenerationProgressEventArgs(message, percent));
    }
}
