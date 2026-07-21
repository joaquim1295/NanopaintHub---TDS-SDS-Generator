namespace NanopaintHub.Domain.Common;

public class CompanyInfoOptions
{
    public const string SectionName = "CompanyInfo";
    
    public string Name { get; set; } = "NanoPaint, Lda.";
    public string Address { get; set; } = "CIIES – Sala 23/28\nAvenida de Tibães, 1199\n4770-568 Vale de São Cosme, Portugal";
    public string Telephone { get; set; } = string.Empty;
    public string EmergencyTelephone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ResponsiblePerson { get; set; } = string.Empty;
}
