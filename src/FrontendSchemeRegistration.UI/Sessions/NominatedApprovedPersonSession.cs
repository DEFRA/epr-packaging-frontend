namespace FrontendSchemeRegistration.UI.Sessions
{
    public class NominatedApprovedPersonSession
    {
        public List<string> Journey { get; set; } = new();

        public string RoleInOrganisation { get; set; }

        public string TelephoneNumber { get; set; }

        public bool IsNominationSubmittedSuccessfully { get; set; }
    }
}
