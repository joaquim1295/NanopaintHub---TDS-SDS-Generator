namespace NanopaintHub.Application.Features.SupplierProducts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Application.Contracts;

public class SaveSupplierProductUseCase : ISaveSupplierProductUseCase
{
    private readonly ISupplierProductRepository _supplierProductRepository;
    private readonly IEchaSubstanceRepository _echaSubstanceRepository;
    private readonly ISdsPdfExtractorService _sdsPdfExtractorService;
    private readonly IUnitOfWork _unitOfWork;

    public SaveSupplierProductUseCase(
        ISupplierProductRepository supplierProductRepository,
        IEchaSubstanceRepository echaSubstanceRepository,
        ISdsPdfExtractorService sdsPdfExtractorService,
        IUnitOfWork unitOfWork)
    {
        _supplierProductRepository = supplierProductRepository;
        _echaSubstanceRepository = echaSubstanceRepository;
        _sdsPdfExtractorService = sdsPdfExtractorService;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        SupplierProduct product,
        string? sdsFilePath,
        List<SupplierProductComposition> uiCompositions,
        bool isNewProduct)
    {
        // 1. Process SDS PDF if provided
        if (!string.IsNullOrWhiteSpace(sdsFilePath))
        {
            var extracted = await _sdsPdfExtractorService.ExtractSdsDataAsync(sdsFilePath);
            foreach (var casData in extracted.CasNumbers)
            {
                var sub = await _echaSubstanceRepository.GetByCasAsync(casData.CasNumber);
                if (sub == null)
                {
                    sub = new EchaSubstance
                    {
                        CasNumber = casData.CasNumber,
                        ChemicalName = $"Substância Importada (CAS: {casData.CasNumber})"
                    };
                    await _echaSubstanceRepository.AddAsync(sub);
                    
                    // Commit to persist the new substance so that the DbContext tracks it correctly as Unchanged.
                    await _unitOfWork.CommitAsync();
                }

                if (casData.ExtractedHazards != null && casData.ExtractedHazards.Any())
                {
                    bool hazardsAdded = false;
                    foreach (var hazard in casData.ExtractedHazards)
                    {
                        if (!sub.Hazards.Any(h => h.PhraseCode == hazard))
                        {
                            sub.Hazards.Add(new SubstanceHazard
                            {
                                Substance = sub,
                                PhraseCode = hazard
                            });
                            hazardsAdded = true;
                        }
                    }

                    if (hazardsAdded)
                    {
                        await _unitOfWork.CommitAsync();
                    }
                }

                // Merge into uiCompositions
                var existingComp = uiCompositions.FirstOrDefault(c => c.EchaSubstanceId == sub.Id);
                if (existingComp != null)
                {
                    if (!existingComp.IsManuallyEdited)
                    {
                        existingComp.Percentage = casData.ParsedPercentage;
                        existingComp.PercentageText = casData.RawPercentageText;
                        existingComp.OriginalPercentageText = casData.RawPercentageText;
                    }
                }
                else
                {
                    var newComp = new SupplierProductComposition
                    {
                        EchaSubstanceId = sub.Id,
                        EchaSubstance = sub,
                        Percentage = casData.ParsedPercentage,
                        PercentageText = casData.RawPercentageText,
                        OriginalPercentageText = casData.RawPercentageText,
                        IsManuallyEdited = false
                    };
                    uiCompositions.Add(newComp);
                }
            }
        }

        // 2. Synchronize Compositions
        if (product.Compositions == null)
        {
            product.Compositions = new List<SupplierProductComposition>();
        }

        // Remove deleted items
        var currentIds = uiCompositions.Select(c => c.Id).ToList();
        var toRemove = product.Compositions.Where(c => !currentIds.Contains(c.Id)).ToList();
        foreach (var rm in toRemove)
        {
            product.Compositions.Remove(rm);
        }

        // Update existing or add new
        foreach (var uiComp in uiCompositions)
        {
            var existing = product.Compositions.FirstOrDefault(c => c.Id == uiComp.Id && c.Id != Guid.Empty);
            if (existing != null)
            {
                existing.Percentage = uiComp.Percentage;
                existing.PercentageText = uiComp.PercentageText;
                existing.OriginalPercentageText = uiComp.OriginalPercentageText;
                existing.IsManuallyEdited = uiComp.IsManuallyEdited;
            }
            else
            {
                product.Compositions.Add(new SupplierProductComposition
                {
                    EchaSubstanceId = uiComp.EchaSubstanceId,
                    Percentage = uiComp.Percentage,
                    PercentageText = uiComp.PercentageText,
                    OriginalPercentageText = uiComp.OriginalPercentageText,
                    IsManuallyEdited = uiComp.IsManuallyEdited
                });
            }
        }

        // 3. Persist
        if (isNewProduct)
        {
            await _supplierProductRepository.AddAsync(product);
        }
        else
        {
            await _supplierProductRepository.UpdateAsync(product);
        }

        await _unitOfWork.CommitAsync();
    }
}
