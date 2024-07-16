namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    public class RecyclingNoteStatusViewModel
    {
        public bool IsPern { get; set; }

        public string Status { get; set; }

        public string IssuedBy { get; set; }

        public string ReproccessingSite { get; set; }

        public string AuthorisedBy { get; set; }

        public string StatusMeaning => IsPern ? "The producer or compliance scheme accepted the PERN." :
                                               "The producer or compliance scheme accepted the PRN.";

        public string AccreditationNumber { get; set; }

        public bool DisplayReporcessingSite { get; set; }
    }
}
