using System.Globalization;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.Extensions.Localization;

namespace FrontendSchemeRegistration.UI.Services
{
    public class PrnService
    {
        private readonly IStringLocalizer<PrnWordsAndPhrases> _localizer;
        private readonly List<PrnViewModel> _prns;

        public PrnService(IStringLocalizer<PrnWordsAndPhrases> localizer)
        {
            _localizer = localizer;
            _prns = new List<PrnViewModel>();

            _prns.Add(GeneratePrn("ER3484743570M", "PRN", "20 Nov 2025", false, "XYZ Reprocessing", 65, "na", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("EX3484743570M", "PERN", "20 Nov 2025", false, "Exporting International", 151, "na", "Paper and board", "ACCEPTED"));
            _prns.Add(GeneratePrn("EX3484743571M", "PERN", "20 Nov 2025", false, "Exporting International", 100, "na", "Wood", "ACCEPTED"));
            _prns.Add(GeneratePrn("EX3484743572M", "PERN", "20 Nov 2025", false, "Exporting International", 60, "na", "Paper and board", "ACCEPTED"));
            _prns.Add(GeneratePrn("EX3484743573M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 70, "na", "Paper and board", "ACCEPTED"));
            _prns.Add(GeneratePrn("EX3484743574M", "PERN", "20 Nov 2024", false, "Packaging reprocessing Ltd", 100, "na", "Plastic", "REJECTED"));
            _prns.Add(GeneratePrn("EX3484743575M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 80, "na", "Paper and board", "CANCELLED"));
            _prns.Add(GeneratePrn("EX3484743576M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 65, "na", "Wood", "ACCEPTED"));
            _prns.Add(GeneratePrn("EX3484743577M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 70, "na", "Paper and board", "ACCEPTED"));

            _prns.Add(GeneratePrn("ER454545540M", "PRN", "20 Nov 2025", false, "XYZ Reprocessing", 65, "Purchase order number 34XFY68", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("EX454545540M", "PERN", "20 Dec 2024", true, "Exporting International", 151, "T2E reference 5689344....", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("EX454545560M", "PERN", "01 Nov 2025", false, "Packaging reprocessing Ltd", 20, "Not provided", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("ER454545540M", "PRN", "10 Nov 2025", false, "Paperlink International Ltd", 100, "Ref 345678F", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("ER454545540M", "PRN", "10 Nov 2025", false, "Monoworlded Recycling Ltd", 100, "Ref 345678F", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("ER454545540M", "PRN", "10 Nov 2025", false, "Bolted Brothers Ltd", 100, "Ref 345678F", "Paper and board", "AWAITING ACCEPTANCE"));

            _prns.Add(GeneratePrn("ER454545540M", "PRN", "20 Nov 2025", false, "XYZ Reprocessing", 65, "Purchase order number 34XFY68", "Wood", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("EX454545540M", "PERN", "20 Dec 2024", true, "Exporting International", 151, "T2E reference 5689344....", "Wood", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("ER454545540M", "PRN", "01 Nov 2025", false, "Packaging reprocessing Ltd", 20, "Not provided", "Wood", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn("ER454545540M", "PRN", "10 Nov 2025", false, "Paperlink International Ltd", 100, "Ref 345678F", "Wood", "AWAITING ACCEPTANCE"));
        }

        // Used by "View all PRNs and PERNs" page
        public PrnListViewModel GetAllPrns()
        {
            var model = new PrnListViewModel();
            model.Prns = _prns.Take(9).ToList();
            return model;
        }

        // Used by "Accept or reject PRNs and PERNs" page
        public PrnListViewModel GetPrnsAwaitingAcceptance()
        {
            var model = new PrnListViewModel();
            model.Prns = _prns.Where(x => x.ApprovalStatus == "AWAITING ACCEPTANCE").ToList();
            return model;
        }

        public PrnViewModel GetPrnByNumber(string prnOrPernNumber)
        {
            var model = _prns.SingleOrDefault(x => x.PrnOrPernNumber == prnOrPernNumber);
            return model;
        }

        private PrnViewModel GeneratePrn(string number, string type, string dateIssued, bool isDecemberWaste, string issuedBy, int tons, string note, string material, string status)
        {
            return new PrnViewModel
            {
                PrnOrPernNumber = number,
                NoteType = type,
                DateIssued = DateTime.Parse(dateIssued, new CultureInfo("en-GB", true)),
                IsDecemberWaste = isDecemberWaste,
                IssuedBy = issuedBy,
                Tonnage = tons,
                AdditionalNotes = note,
                Material = _localizer[material.ToLowerInvariant()],
                ApprovalStatus = status,
                ReproccessingSite = "23 Ruby Street, London, NW N32",
                AuthorisedBy = "John Smith",
                AccreditationNumber = "ER123456789",
                ProducerOrComplianceScheme = "Tesco",
                DecemberWasteDisplay = isDecemberWaste ? _localizer["Yes"] : _localizer["No"]
            };
        }
    }
}
