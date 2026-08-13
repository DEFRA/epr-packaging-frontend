using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

/// <summary>
/// SUB-345: a resubmission the regulator has already ruled on, shown as a look-back rather than a journey.
/// </summary>
[ExcludeFromCodeCoverage]
public class CompletedResubmissionViewModel
{
    public string OrganisationName { get; set; } = string.Empty;

    public bool IsComplianceScheme { get; set; }

    public bool IsApprovedOrDelegatedUser { get; set; }

    public Guid? SubmissionId { get; set; }

    public string? ApplicationReferenceNumber { get; set; }

    public string? FileName { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public string? SubmittedBy { get; set; }

    public DateTime? DeclarationDate { get; set; }

    public string? RegulatorComments { get; set; }

    /// <summary>
    /// SUB-345: the fee breakdown for the completed cycle, or null when the fee could not be read.
    /// </summary>
    /// <remarks>
    /// The page is a look-back, so a fee the payment service cannot price today - member details withdrawn,
    /// for instance - must leave the rest of the resubmission readable rather than fail the page.
    /// </remarks>
    public ResubmissionFeeViewModel? Fee { get; set; }
}
