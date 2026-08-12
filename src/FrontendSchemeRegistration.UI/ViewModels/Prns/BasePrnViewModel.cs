using FrontendSchemeRegistration.Application.Constants;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
	using Constants;

	public class BasePrnViewModel
    {
        public Guid ExternalId { get; set; }

        public string PrnOrPernNumber { get; set; }

        // e.g. Wood, Paper and board, etc.
        public string Material { get; set; }

        public string MaterialGroup => Material == PrnConstants.Material.Fibre ? PrnConstants.Material.Paper : Material;

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

        /// <summary>
        ///     True when the PRN currently offers a choice of obligation years to accept into
        ///     (December Waste within the Dec/Jan window for 2026 onwards).
        /// </summary>
        public bool HasChoiceOfAcceptanceYear => AvailableAcceptanceYears.Length == 2;

        public bool IsStatusEditable => ApprovalStatus == PrnStatus.AwaitingAcceptance
			&& AvailableAcceptanceYears.Length > 0;

        public int ObligationYear { get; set; }

        public string AdditionalNotes { get; set; }

        public string NoteType { get; set; }

        public bool IsAwaitingAcceptance => ApprovalStatus == PrnStatus.AwaitingAcceptance;

        /// <summary>
        ///     True when the user is viewing during the UK December/January flash window
        ///     and this PRN was issued in that same immediate Dec/Jan period.
        /// </summary>
        public bool IsInDecemberWasteFlashWindow { get; set; }

        /// <summary>
        ///     December waste flash is only shown for awaiting-acceptance December waste PRNs
        ///     during Dec/Jan for that waste year, when at least one acceptance year is available.
        /// </summary>
        public bool ShouldShowDecemberWasteFlash =>
            IsDecemberWaste
            && IsAwaitingAcceptance
            && IsInDecemberWasteFlashWindow
            && AvailableAcceptanceYears.Length > 0;

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