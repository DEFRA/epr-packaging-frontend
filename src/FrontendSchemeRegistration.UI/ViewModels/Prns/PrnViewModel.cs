namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Packaging Recovery Note (PRN) or Packaging Waste Export Recycling Note (PERN).
    /// </summary>
    public class PrnViewModel
    {
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

        // True if selected in the user interface.
        public bool IsSelected { get; set; }

        public string DecemberWasteDisplay => IsDecemberWaste ? "Yes" : "No";

        public string DateIssuedDisplay => DateIssued.ToString("dd MMM yyyy");
    }
}
