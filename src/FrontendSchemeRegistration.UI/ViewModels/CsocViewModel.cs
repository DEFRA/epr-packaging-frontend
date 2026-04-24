namespace FrontendSchemeRegistration.UI.ViewModels;

using Application.Enums;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CsocViewModel
{
    public bool IsApprovedUser { get; set; }
    public bool IsDirectProducer { get; set; }
    public bool IsComplianceScheme { get; set; }
    public DateTime SubmissionDeadline { get; set; }
    public int ComplianceYear { get; set; }
    public string? UnderstandingObligationsEndpoint { get; set; }
    public bool IsObligationDataSubmitted { get; set; }
    public ComplianceDeclarationStatus? ComplianceDeclarationStatus { get; set; }
}