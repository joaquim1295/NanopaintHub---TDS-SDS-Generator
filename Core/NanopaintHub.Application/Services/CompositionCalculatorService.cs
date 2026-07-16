namespace NanopaintHub.Application.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

public class CompositionCalculatorService : ICompositionCalculatorService
{
    private readonly ISupplierProductRepository _supplierProductRepository;

    public CompositionCalculatorService(ISupplierProductRepository supplierProductRepository)
    {
        _supplierProductRepository = supplierProductRepository;
    }

    public async Task<List<FlattenedComposition>> FlattenProductCompositionsAsync(Product product)
    {
        var flatList = new Dictionary<Guid, FlattenedComposition>();

        foreach (var comp in product.Compositions)
        {
            if (comp.EchaSubstanceId.HasValue && comp.EchaSubstance != null)
            {
                // Pure substance
                AddOrUpdateSubstance(flatList, comp.EchaSubstance, comp.Percentage);
            }
            else if (comp.SupplierProductId.HasValue)
            {
                // Mixture / Raw Material
                var supplierProduct = await _supplierProductRepository.GetByIdAsync(comp.SupplierProductId.Value);
                if (supplierProduct != null)
                {
                    foreach (var supplierComp in supplierProduct.Compositions)
                    {
                        if (supplierComp.EchaSubstance != null)
                        {
                            // Flattening algorithm: (Percentage in product * Percentage in raw material) / 100
                            double actualPercentage = (comp.Percentage * supplierComp.Percentage) / 100.0;
                            AddOrUpdateSubstance(flatList, supplierComp.EchaSubstance, actualPercentage);
                        }
                    }
                }
            }
        }

        return flatList.Values.OrderByDescending(v => v.FinalPercentage).ToList();
    }

    private void AddOrUpdateSubstance(Dictionary<Guid, FlattenedComposition> dict, EchaSubstance substance, double percentage)
    {
        if (dict.TryGetValue(substance.Id, out var existing))
        {
            existing.FinalPercentage += percentage;
        }
        else
        {
            dict[substance.Id] = new FlattenedComposition
            {
                Substance = substance,
                FinalPercentage = percentage
            };
        }
    }
}
