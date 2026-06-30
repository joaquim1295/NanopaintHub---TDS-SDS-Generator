namespace NanopaintHub.Application.Contracts;

using System.Threading;
using System.Threading.Tasks;

public interface IEchaImportService
{
    Task<int> ImportCsvAsync(string filePath, CancellationToken cancellationToken = default);
}
