using System.Collections;

namespace FrontendSchemeRegistration.UI.Sessions
{
    public class PrnSession
    {
        public List<Guid> SelectedPrnIds { get; set; } = new();

        public string InitialNoteTypes { get; set; }

        public Hashtable Backlinks { get; set;} = new();

        public int? SelectedObligationYear { get; set; }

        /// <summary>
        ///     PRN the user is currently accepting when they have chosen an obligation year.
        /// </summary>
        public Guid? SelectedAcceptanceYearPrnId { get; set; }

        /// <summary>
        ///     Obligation year chosen on the December Waste "choose which year to accept into" page.
        /// </summary>
        public int? SelectedAcceptanceYear { get; set; }
    }
}
