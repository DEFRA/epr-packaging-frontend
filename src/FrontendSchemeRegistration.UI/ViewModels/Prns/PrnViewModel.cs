using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Prns;
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

        // Gets or sets get or sets value, either PRN or PERN.
        public string NoteType { get; set; }

        public string AdditionalNotes { get; set; }

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

        // Refer to Resources/Views/Shared/Partials/Prns/_recyclingNoteDetails.en.resx and _recyclingNoteDetails.cy.resx
        public string ApprovalStatusExplanationTranslation
        {
            get
            {
                switch (ApprovalStatus)
                {
                    case "AWAITINGACCEPTANCE":
                        return string.Concat(IsPrn ? "prn" : "pern", "_awaiting_acceptance_explanation");
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

        public static implicit operator PrnViewModel(PrnModel prn)
        {
            return new PrnViewModel
            {
                Id = prn.Id,
                ExternalId = prn.ExternalId,
                PrnOrPernNumber = prn.PrnNumber,
                NoteType = prn.IsExport ? PrnConstants.PernText : PrnConstants.PrnText,
                DateIssued = prn.IssueDate,
                IsDecemberWaste = prn.DecemberWaste,
                IssuedBy = prn.IssuedByOrg,
                Tonnage = prn.TonnageValue,
                AdditionalNotes = prn.IssuerNotes,
                Material = prn.MaterialName,
                ApprovalStatus = MapStatus(prn.PrnStatus),
                ReproccessingSiteAddress = prn.ReprocessingSite,
                AuthorisedBy = prn.PrnSignatory,
                AccreditationNumber = prn.AccreditationNumber,
                NameOfProducerOrComplianceScheme = prn.OrganisationName,

                // CancelledDate = prn.CancelledDate,
                // CreatedOn = prn.CreatedOn,
                // CreatedBy = prn.CreatedBy,
                // IsExport = prn.IsExport,
                // IssuerReference = prn.IssuerReference,
                // LastUpdatedBy = prn.LastUpdatedBy,
                // LastUpdatedDate = prn.LastUpdatedDate,
                // ObligationYear = int.Parse(prn.ObligationYear)
                // OrganisationId = prn.OrganisationId,
                // PackagingProducer = prn.PackagingProducer,
                // PrnSignatoryPosition = prn.PrnSignatoryPosition,
                // ProcessToBeUsed = prn.ProcessToBeUsed,
                // ProducerAgency = prn.ProducerAgency,
                // ReprocessorExporterAgency = prn.ReprocessorExporterAgency,
                // Signature = prn.Signature,
            };
        }

        public bool DecemberWasteRulesApply(DateTime now)
        {
            return IsDecemberWaste
                && (IssueYear == now.Year || (IssueYear == now.Year - 1 && now.Month == 1));
        }
    }
}
