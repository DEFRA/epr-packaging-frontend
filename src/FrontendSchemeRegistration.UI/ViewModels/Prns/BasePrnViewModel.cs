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

        public string ApprovalStatusDisplayCssColour
        {
            get
            {
                switch (ApprovalStatus)
                {
                    case PrnStatus.AwaitingAcceptance:
                        return "grey";
                    case PrnStatus.Accepted:
                        return "green";
                    case PrnStatus.Cancelled:
                        return "yellow";
                    case PrnStatus.Rejected:
                        return "red";
                    default:
                        return "grey";
                }
            }
        }

        public static string MapStatus(string oldStatus)
        {
            if (oldStatus == "AWAITINGACCEPTANCE")
            {
                return PrnStatus.AwaitingAcceptance;
            }
            else if (oldStatus == "CANCELED")
            {
                return PrnStatus.Cancelled;
            }
            else
            {
                return oldStatus;
            }

        }
    }
}
