using System.Threading.Tasks;

namespace NanopaintHub.Application.Contracts;

public interface IHtmlToPdfGenerator
{
    Task<byte[]> GeneratePdfAsync(string html, string pageFormat, bool isLandscape);
}
