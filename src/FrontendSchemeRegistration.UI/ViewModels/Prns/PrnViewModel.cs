using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Packaging Recovery Note (PRN) or Packaging Waste Export Recycling Note (PERN).
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PrnViewModel : BasePrnViewModel
    {
        public int Id { get; set; }

        // Address of site, applies to PRNs only
        public string ReproccessingSiteAddress { get; set; }

        // Name of person who authorised the PRN or PERN
        public string AuthorisedBy { get; set; }

        public string AccreditationNumber { get; set; }

        // E.g. Tesco
        public string NameOfProducerOrComplianceScheme { get; set; }

        // Unclear how Year is obtained
        public int IssueYear => DateIssued.Year;

        public bool IsPrn => NoteType == PrnConstants.PrnText;

        // True if selected in the user interface.
        public bool IsSelected { get; set; }

        // SignatoryPosition from prn
        public string Position { get; set; }

        public string RecyclingProcess { get; set; }

        // Unreliable, actually the cancellation date. Needs work in backend to make reliable
        public DateTime? StatusUpdatedOn { get; set; }

        // Use because StatusUpdatedOn cannot be relied on
        public DateTime LastUpdatedDate { get; set; }

        // Refer to Resources/Views/Shared/Partials/Prns/_recyclingNoteDetails.en.resx and _recyclingNoteDetails.cy.resx
        public string ApprovalStatusExplanationTranslation
        {
            get
            {
                switch (ApprovalStatus)
                {
                    case PrnStatus.AwaitingAcceptance:
                        return string.Concat(IsPrn ? "prn" : "pern", "_awaiting_acceptance_explanation");
                    case PrnStatus.Accepted:
                        return string.Concat(IsPrn ? "prn" : "pern", "_accepted_explanation");
                    case PrnStatus.Cancelled:
                        return string.Concat(IsPrn ? "prn" : "pern", "_cancelled_explanation");
                    case PrnStatus.Rejected:
                        return string.Concat(IsPrn ? "prn" : "pern", "_rejected_explanation");
                    default:
                        return "missing_status_explanation";
                }
            }
        }
    }
}