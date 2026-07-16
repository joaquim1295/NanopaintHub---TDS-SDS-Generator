namespace NanopaintHub.Application.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;
using NanopaintHub.Domain.Entities;

public interface ICompositionCalculatorService
{
    /// <summary>
    /// Flattens the compositions of a product. 
    /// If the product contains raw materials (SupplierProducts), it traverses down 
    /// and multiplies percentages to return a flat list of EchaSubstance percentages.
    /// </summary>
    Task<List<FlattenedComposition>> FlattenProductCompositionsAsync(Product product);
}

public class FlattenedComposition
{
    public EchaSubstance Substance { get; set; } = null!;
    public double FinalPercentage { get; set; }
}
