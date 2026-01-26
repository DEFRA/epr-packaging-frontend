using FrontendSchemeRegistration.Application.Constants;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    public class BasePrnViewModel
    {
        public Guid ExternalId { get; set; }

        public string PrnOrPernNumber { get; set; }

        // e.g. Wood, Paper and board, etc.
        public string Material { get; set; }

        public DateTime DateIssued { get; set; }

        public bool IsDecemberWaste { get; set; }

        public string IssuedBy { get; set; }

        public int Tonnage { get; set; }

        // e.g. AWAITING ACCEPTANCE, ACCEPTED, REJECTED, CANCELLED
        public string ApprovalStatus { get; set; }

        public string DecemberWasteDisplay => IsDecemberWaste ? "Yes" : "No";

        public string DateIssuedDisplay => DateIssued.ToString("dd MMM yyyy");

        /// <summary>
        ///		Contains any years that the PRN may be accepted against at this moment in time.
        ///		May be empty - if so, the user should not be able to action any changes against this PRN.
        /// </summary>
        public int[] AvailableAcceptanceYears { get; set; } = [];

        public bool IsStatusEditable => ApprovalStatus == PrnStatus.AwaitingAcceptance
			&& AvailableAcceptanceYears.Length > 0;

        public int ObligationYear { get; set; }

        public string AdditionalNotes { get; set; }

        public string NoteType { get; set; }

        public string ApprovalStatusDisplayCssColour => ApprovalStatus switch
        {
	        PrnStatus.AwaitingAcceptance => "grey",
	        PrnStatus.Accepted => "green",
	        PrnStatus.Cancelled => "yellow",
	        PrnStatus.Rejected => "red",
	        _ => "grey"
        };


        public static string MapStatus(string oldStatus) => oldStatus switch
		{
			"AWAITINGACCEPTANCE" => PrnStatus.AwaitingAcceptance,
			"CANCELED" => PrnStatus.Cancelled,
			_ => oldStatus
		};
	}
}