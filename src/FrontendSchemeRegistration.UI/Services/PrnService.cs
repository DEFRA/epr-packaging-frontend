using System.Globalization;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;

namespace FrontendSchemeRegistration.UI.Services
{
    public class PrnService : IPrnService
    {
        public AcceptedPernsOrPrnsViewModel? GetPrn(string prnOrPernNumber)
        {
            var model = new AcceptedPernsOrPrnsViewModel()
            {
                Status = "accepted",
                PrnOrPernNumber = "EX4545452026",
                Tonnage = 170,
                Material = "paper and board",
                Year = 2024,
                DateIssued = DateTime.Now,
                AuthorisedBy = "PRNAuthoriser",
                IsDecemberWaste = false,
                IsPern = false,
                IssuedBy = "PRNIssuer",
                Note = "Note123",
                ProducerOrComplianceSchemeName = "Tesco",
                AccreditationNumber = "TEC12345",
                ReproccessingSite = "Repo123"
            };

            return model;
        }

        public PrnListViewModel GetPrns()
        {
            var prns = new PrnListViewModel();
            prns.Prns = new List<PrnViewModel>();

            prns.Prns.Add(GeneratePrn("ER454545540M", "20 Nov 2025", false, "XYZ Reprocessing", 65, "Purchase order number 34XFY68", "Paper and board"));
            prns.Prns.Add(GeneratePrn("EX454545540M", "20 Dec 2024", true, "Exporting International", 151, "T2E reference 5689344....", "Paper and board"));
            prns.Prns.Add(GeneratePrn("EX454545560M", "01 Nov 2025", false, "Packaging reprocessing Ltd", 20, "Not provided", "Paper and board"));
            prns.Prns.Add(GeneratePrn("ER454545540M", "10 Nov 2025", false, "Paperlink International Ltd", 100, "Ref 345678F", "Paper and board"));
            prns.Prns.Add(GeneratePrn("ER454545540M", "10 Nov 2025", false, "Monoworlded Recycling Ltd", 100, "Ref 345678F", "Paper and board"));
            prns.Prns.Add(GeneratePrn("ER454545540M", "10 Nov 2025", false, "Bolted Brothers Ltd", 100, "Ref 345678F", "Paper and board"));

            prns.Prns.Add(GeneratePrn("ER454545540M", "20 Nov 2025", false, "XYZ Reprocessing", 65, "Purchase order number 34XFY68", "Wood"));
            prns.Prns.Add(GeneratePrn("EX454545540M", "20 Dec 2024", true, "Exporting International", 151, "T2E reference 5689344....", "Wood"));
            prns.Prns.Add(GeneratePrn("ER454545540M", "01 Nov 2025", false, "Packaging reprocessing Ltd", 20, "Not provided", "Wood"));
            prns.Prns.Add(GeneratePrn("ER454545540M", "10 Nov 2025", false, "Paperlink International Ltd", 100, "Ref 345678F", "Wood"));
            return prns;
        }

        private PrnViewModel GeneratePrn(string number, string dateIssued, bool isDecemberWaste, string issuedBy, int tons, string note, string material)
        {
            return new PrnViewModel
            {
                PrnOrPernNumber = number,
                DateIssued = DateTime.Parse(dateIssued, new CultureInfo("en-GB", true)),
                IsDecemberWaste = isDecemberWaste,
                IssuedBy = issuedBy,
                Tonnage = tons,
                Note = note,
                Material = material
            };
        }
    }
}
