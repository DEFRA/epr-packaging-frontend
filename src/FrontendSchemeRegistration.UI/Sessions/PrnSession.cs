using System.Collections;

namespace FrontendSchemeRegistration.UI.Sessions
{
    public class PrnSession
    {
        public List<Guid> SelectedPrnIds { get; set; } = new();

        public string InitialNoteTypes { get; set; }

        public Hashtable Backlinks { get; set;} = new();
    }
}
