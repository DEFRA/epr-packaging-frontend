namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CsocViewModel
{
    public bool IsApprovedUser { get; set; }
    public bool IsDirectProducer { get; set; }
    public bool IsComplianceScheme { get; set; }
    public DateTime SubmissionDeadline { get; set; }
}