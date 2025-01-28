namespace FrontendSchemeRegistration.UI.ViewModels.NominatedApprovedPerson
{
    public class ConfirmDetailsApprovedPersonViewModel
    {
        public Guid Id { get; set; }

        public string? RoleInOrganisation { get; set; }

        public string? TelephoneNumber { get; set; }

        public string? RoleChangeUrl { get; set; }

        public string? TelephoneChangeUrl { get; set; }

        public bool IsInCompanyHouse { get; set; }
    }
}
