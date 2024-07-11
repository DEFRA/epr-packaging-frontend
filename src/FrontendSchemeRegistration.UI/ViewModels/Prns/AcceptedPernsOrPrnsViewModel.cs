namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    public class AcceptedPernsOrPrnsViewModel
    {
        public bool IsPern { get; set; }

        public string PrnOrPernNumber { get; set; }

        public int Year { get; set; }

        public int Tonnage { get; set; }

        public DateTime DateIssued { get; set; }

        public string IssuedBy { get; set; }

        public string AuthorisedBy { get; set; }

        public bool IsDecemberWaste { get; set; }

        public string DecemberWasteDisplay => IsDecemberWaste ? "Yes" : "No";

        public string DateIssuedDisplay => DateIssued.ToString("dd MMM yyyy");

        public string Material { get; set; }

        public string Note { get; set; }

        public string ProducerOrComplianceSchemeNumber { get; set; }

        public string ProducerOrComplianceSchemeName { get; set; }

        public string ReproccessingSite { get; set; }
    }
}
