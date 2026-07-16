namespace NanopaintHub.Application.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface ISaveSupplierProductUseCase
{
    Task ExecuteAsync(
        SupplierProduct product,
        string? sdsFilePath,
        List<SupplierProductComposition> uiCompositions,
        bool isNewProduct);
}
