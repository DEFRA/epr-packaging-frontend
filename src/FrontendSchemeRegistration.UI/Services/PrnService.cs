using System.Globalization;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.Extensions.Localization;

namespace FrontendSchemeRegistration.UI.Services
{
    public class PrnService : IPrnService
    {
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IStringLocalizer<PrnDataResources> _prnDataLocalizer;
        private readonly List<PrnViewModel> _prns;

        public PrnService(IStringLocalizer<SharedResources> localizer, IStringLocalizer<PrnDataResources> prnDatalocalizer)
        {
            _localizer = localizer;
            _prnDataLocalizer = prnDatalocalizer;
            _prns = new List<PrnViewModel>();

            _prns.Add(GeneratePrn(1, "ER3484743570M", "PRN", "20 Nov 2025", false, "XYZ Reprocessing", 65, "na", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(2, "EX3484743570M", "PERN", "20 Nov 2025", false, "Exporting International", 151, "na", "Paper and board", "ACCEPTED"));
            _prns.Add(GeneratePrn(3, "EX3484743571M", "PERN", "20 Nov 2025", false, "Exporting International", 100, "na", "Wood", "ACCEPTED"));
            _prns.Add(GeneratePrn(4, "EX3484743572M", "PERN", "20 Nov 2025", false, "Exporting International", 60, "na", "Paper and board", "ACCEPTED"));
            _prns.Add(GeneratePrn(5, "EX3484743573M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 70, "na", "Paper and board", "ACCEPTED"));
            _prns.Add(GeneratePrn(6, "EX3484743574M", "PERN", "20 Nov 2024", false, "Packaging reprocessing Ltd", 100, "na", "Plastic", "REJECTED"));
            _prns.Add(GeneratePrn(7, "EX3484743575M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 80, "na", "Paper and board", "CANCELLED"));
            _prns.Add(GeneratePrn(8, "EX3484743576M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 65, "na", "Wood", "ACCEPTED"));
            _prns.Add(GeneratePrn(9, "EX3484743577M", "PERN", "20 Nov 2024", true, "Packaging reprocessing Ltd", 70, "na", "Paper and board", "ACCEPTED"));

            _prns.Add(GeneratePrn(10, "ER454545540M", "PRN", "20 Nov 2025", false, "XYZ Reprocessing", 65, "Purchase order number 34XFY68", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(11, "EX454545540M", "PERN", "20 Dec 2024", true, "Exporting International", 151, "T2E reference 5689344....", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(12, "EX454545560M", "PERN", "01 Nov 2025", false, "Packaging reprocessing Ltd", 20, "Not provided", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(13, "ER454545540M", "PRN", "10 Nov 2025", false, "Paperlink International Ltd", 100, "Ref 345678F", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(13, "ER454545540M", "PRN", "10 Nov 2025", false, "Monoworlded Recycling Ltd", 100, "Ref 345678F", "Paper and board", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(15, "ER454545540M", "PRN", "10 Nov 2025", false, "Bolted Brothers Ltd", 100, "Ref 345678F", "Paper and board", "AWAITING ACCEPTANCE"));

            _prns.Add(GeneratePrn(16, "ER454545540M", "PRN", "20 Nov 2025", false, "XYZ Reprocessing", 65, "Purchase order number 34XFY68", "Wood", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(17, "EX454545540M", "PERN", "20 Dec 2024", true, "Exporting International", 151, "T2E reference 5689344....", "Wood", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(18, "ER454545540M", "PRN", "01 Nov 2025", false, "Packaging reprocessing Ltd", 20, "Not provided", "Wood", "AWAITING ACCEPTANCE"));
            _prns.Add(GeneratePrn(19, "ER454545540M", "PRN", "10 Nov 2025", false, "Paperlink International Ltd", 100, "Ref 345678F", "Wood", "AWAITING ACCEPTANCE"));

            if (CurrentPrns == null)
            {
                CurrentPrns = _prns;
            }
        }

        public static List<PrnViewModel> CurrentPrns { get; set; }

        // Used by "View all PRNs and PERNs" page
        public PrnListViewModel GetAllPrns()
        {
            var model = new PrnListViewModel();
            model.Prns = CurrentPrns.Take(9).ToList();
            return model;
        }

        // Used by "Accept or reject PRNs and PERNs" page
        public PrnListViewModel GetPrnsAwaitingAcceptance()
        {
            var model = new PrnListViewModel();
            model.Prns = CurrentPrns.Where(x => x.ApprovalStatus == "AWAITING ACCEPTANCE").ToList();
            return model;
        }

        public PrnViewModel GetPrnById(int id)
        {
            var model = CurrentPrns.Single(x => x.Id == id);
            return model;
        }

        public PrnAcceptViewModel GetAcceptPrnById(int id)
        {
            var prnAccept = new PrnAcceptViewModel();
            var prn = CurrentPrns.Single(x => x.Id == id);
            return prnAccept;
        }

        public void UpdatePrnStatus(int id, string approvalStatus)
        {
            var model = CurrentPrns.Single(x => x.Id == id);
            model.ApprovalStatus = approvalStatus;
        }

        private PrnViewModel GeneratePrn(int id, string number, string type, string dateIssued, bool isDecemberWaste, string issuedBy, int tons, string note, string material, string status)
        {
            return new PrnViewModel
            {
                Id = id,
                PrnOrPernNumber = number,
                NoteType = type,
                DateIssued = DateTime.Parse(dateIssued, new CultureInfo("en-GB", true)),
                IsDecemberWaste = isDecemberWaste,
                IssuedBy = issuedBy,
                Tonnage = tons,
                AdditionalNotes = note,
                Material = _prnDataLocalizer?[material],
                ApprovalStatus = _prnDataLocalizer?[status],
                ApprovalStatusExplanation = string.Format(_prnDataLocalizer?[string.Concat(status, " ", "EXPLANATION")], type),
                ReproccessingSiteAddress = "23 Ruby Street, London, NW N32",
                AuthorisedBy = "John Smith",
                AccreditationNumber = "ER123456789",
                NameOfProducerOrComplianceScheme = "Tesco",
                DecemberWasteDisplay = isDecemberWaste ? _localizer["Yes"] : _localizer["No"]
            };
        }
    }
}
