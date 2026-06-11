namespace NanopaintHub.Application.Contracts;

public sealed class PdfGenerationProgressEventArgs : EventArgs
{
    public PdfGenerationProgressEventArgs(string message, double? percent = null)
    {
        Message = message;
        Percent = percent;
    }

    public string Message { get; }
    public double? Percent { get; }
}

public interface IPdfGenerationProgressNotifier
{
    event EventHandler<PdfGenerationProgressEventArgs>? ProgressChanged;
    void Report(string message, double? percent = null);
}
