using FrontendSchemeRegistration.UI.Constants;

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

        public string BuildConfirmationHeading(
            bool multiYearEnabled,
            Func<string, string> getResource,
            Func<string, string> localizeNoteType)
        {
            if (NoteTypes == PrnConstants.PrnText || NoteTypes == PrnConstants.PernText)
            {
                return multiYearEnabled
                    ? string.Format(
                        getResource("you_have_accepted_one_prn_or_pern_multi_year"),
                        localizeNoteType(NoteTypes),
                        ObligationYears)
                    : string.Format(
                        getResource("you_have_accepted_one_prn_or_pern"),
                        localizeNoteType(NoteTypes));
            }

            if (NoteTypes == PrnConstants.PrnsText || NoteTypes == PrnConstants.PernsText)
            {
                return multiYearEnabled
                    ? string.Format(
                        getResource("you_have_accepted_multipe_prn_or_pern_multi_year"),
                        Count,
                        localizeNoteType(NoteTypes),
                        ObligationYears)
                    : string.Format(
                        getResource("you_have_accepted_multipe_prn_or_pern"),
                        Count,
                        localizeNoteType(NoteTypes));
            }

            var noteTypes = NoteTypes.Split(",");

            return multiYearEnabled
                ? string.Format(
                    getResource("you_have_accepted_mix_prns_and_perns_multi_year"),
                    Count,
                    localizeNoteType(noteTypes[0]),
                    localizeNoteType(noteTypes[1]),
                    ObligationYears)
                : string.Format(
                    getResource("you_have_accepted_mix_prns_and_perns"),
                    Count,
                    localizeNoteType(noteTypes[0]),
                    localizeNoteType(noteTypes[1]));
        }
    }

    public record AcceptedDetails(string Material, int Tonnage);
}
