namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    public class AcceptedPrnsModel
    {
        public int Count { get; set; }

        public List<AcceptedDetails> Details { get; set; }

        public string NoteTypes { get; set; }

        // All accepted Prns should have the same obligation year so in theory this is not required
        // In early stages of development howevevr, there is nothing to prevent selection of more than one
        public string ObligationYears { get; set; }
    }

    public record AcceptedDetails(string Material, int Tonnage);
}
