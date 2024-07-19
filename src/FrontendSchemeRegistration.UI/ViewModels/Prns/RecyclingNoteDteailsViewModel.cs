namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    public class RecyclingNoteDteailsViewModel
    {
        public bool IsPern { get; set; }

        public DateTime DateIssued { get; set; }

        public string DateIssuedDisplay => DateIssued.ToString("dd MMM yyyy");

        public bool IsDecemberWaste { get; set; }

        public string DecemberWasteDisplay => IsDecemberWaste ? "Yes" : "No";

        public string Material { get; set; }

        public int Tonnage { get; set; }

        public string ProducerOrComplianceSchemeName { get; set; }

        public string Note { get; set; }
    }
}
