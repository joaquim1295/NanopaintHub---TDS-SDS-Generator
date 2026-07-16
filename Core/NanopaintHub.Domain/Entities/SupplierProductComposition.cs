namespace NanopaintHub.Domain.Entities;

using System;
using NanopaintHub.Domain.Common;

public class SupplierProductComposition : Entity
{
    public Guid SupplierProductId { get; set; }
    public SupplierProduct? SupplierProduct { get; set; }

    public Guid EchaSubstanceId { get; set; }
    public EchaSubstance? EchaSubstance { get; set; }

    public double Percentage { get; set; }
    private string? _percentageText;
    public string? PercentageText 
    { 
        get => _percentageText;
        set
        {
            if (_percentageText != value)
            {
                _percentageText = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string? OriginalPercentageText { get; set; }
    
    private bool _isManuallyEdited;
    public bool IsManuallyEdited
    {
        get => _isManuallyEdited;
        set
        {
            if (_isManuallyEdited != value)
            {
                _isManuallyEdited = value;
                OnPropertyChanged();
            }
        }
    }
}
