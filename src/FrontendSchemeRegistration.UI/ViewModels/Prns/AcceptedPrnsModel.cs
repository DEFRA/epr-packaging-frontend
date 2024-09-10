namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    public class AcceptedPrnsModel
    {
        public int Count { get; set; }

        public List<AcceptedDetails> Details { get; set; }

        public string NoteTypes { get; set; }
    }

    public record AcceptedDetails(string Material, int Tonnage);
}
