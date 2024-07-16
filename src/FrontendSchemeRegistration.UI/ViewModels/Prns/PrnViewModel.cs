namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Packaging Recovery Note (PRN) or Packaging Waste Export Recycling Note (PERN).
    /// </summary>
    public class PrnViewModel
    {
        public int Id { get; set; }

        public string PrnOrPernNumber { get; set; }

        // Gets or sets get or sets value, either PRN or PERN.
        public string NoteType { get; set; }

        public DateTime DateIssued { get; set; }

        public bool IsDecemberWaste { get; set; }

        public string IssuedBy { get; set; }

        public int Tonnage { get; set; }

        public string AdditionalNotes { get; set; }

        // e.g. Wood, Paper and board, etc.
        public string Material { get; set; }

        // e.g. AWAITING ACCEPTANCE, ACCEPTED, REJECTED, CANCELLED
        public string ApprovalStatus { get; set; }

        // Provides description of what ApprovalStatus means
        public string ApprovalStatusExplanation { get; set; }

        // Address of site, applies to PRNs only
        public string ReproccessingSiteAddress { get; set; }

        // Name of person who authorised the PRN or PERN
        public string AuthorisedBy { get; set; }

        public string AccreditationNumber { get; set; }

        // E.g. Tesco
        public string NameOfProducerOrComplianceScheme { get; set; }

        // Unclear how Year is obtained
        public int Year => DateIssued.Year;

        public bool IsPrn => NoteType == "PRN";

        // True if selected in the user interface.
        public bool IsSelected { get; set; }

        public string DecemberWasteDisplay { get; set; }

        public string DateIssuedDisplay => DateIssued.ToString("dd MMM yyyy");

        public string ApprovalStatusDisplayCssColour
        {
            get
            {
                switch (ApprovalStatus)
                {
                    case "AWAITING ACCEPTANCE":
                        return "grey";
                    case "ACCEPTED":
                        return "green";
                    case "CANCELLED":
                        return "yellow";
                    case "REJECTED":
                        return "red";
                    default:
                        return "grey";
                }
            }
        }
    }
}
